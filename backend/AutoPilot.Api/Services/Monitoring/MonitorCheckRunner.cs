using System.Diagnostics;
using AutoPilot.Api.Data;
using AutoPilot.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoPilot.Api.Services.Monitoring;

public sealed class MonitorCheckRunner : IMonitorCheckRunner
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;

    public MonitorCheckRunner(AppDbContext dbContext, IHttpClientFactory httpClientFactory)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<MonitorCheckRun?> RunCheckAsync(Guid monitorId, CancellationToken cancellationToken)
    {
        var monitor = await _dbContext.Monitors.FirstOrDefaultAsync(x => x.Id == monitorId, cancellationToken);
        if (monitor is null)
        {
            return null;
        }

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(20);

        var run = new MonitorCheckRun
        {
            MonitorId = monitor.Id,
            ExecutedAtUtc = DateTime.UtcNow
        };

        var watch = Stopwatch.StartNew();
        try
        {
            using var request = new HttpRequestMessage(new HttpMethod(monitor.Method), monitor.TargetUrl);
            using var response = await client.SendAsync(request, cancellationToken);
            watch.Stop();

            run.ResponseTimeMs = (int)watch.ElapsedMilliseconds;
            run.StatusCode = (int)response.StatusCode;
            run.IsSuccess = response.IsSuccessStatusCode;
            run.ErrorMessage = null;
        }
        catch (Exception ex)
        {
            watch.Stop();
            run.ResponseTimeMs = (int)watch.ElapsedMilliseconds;
            run.StatusCode = null;
            run.IsSuccess = false;
            run.ErrorMessage = ex.Message;
        }

        monitor.LastCheckedAtUtc = run.ExecutedAtUtc;
        monitor.LastCheckSucceeded = run.IsSuccess;
        monitor.LastStatusCode = run.StatusCode;
        monitor.LastResponseTimeMs = run.ResponseTimeMs;
        monitor.LastErrorMessage = run.ErrorMessage;

        if (run.IsSuccess)
        {
            monitor.ConsecutiveSuccessCount += 1;
            monitor.ConsecutiveFailureCount = 0;
        }
        else
        {
            monitor.ConsecutiveFailureCount += 1;
            monitor.ConsecutiveSuccessCount = 0;
        }

        _dbContext.MonitorCheckRuns.Add(run);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return run;
    }
}