using System.Diagnostics;
using System.Security.Claims;
using AutoPilot.Api.Data;
using AutoPilot.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitorEntity = AutoPilot.Api.Models.Monitor;

namespace AutoPilot.Api.Controllers;

[ApiController]
[Route("api/monitors")]
[Authorize]
public class MonitorsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;

    public MonitorsController(AppDbContext dbContext, IHttpClientFactory httpClientFactory)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MonitorListItemResponse>>> List(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var monitors = await _dbContext.Monitors
            .Where(x => x.OwnerUserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new MonitorListItemResponse(
                x.Id,
                x.Name,
                x.TargetUrl,
                x.Method,
                x.CheckIntervalSeconds,
                x.IsActive,
                x.CreatedAtUtc,
                x.CheckRuns
                    .OrderByDescending(run => run.ExecutedAtUtc)
                    .Select(run => new LatestCheckResponse(run.ExecutedAtUtc, run.IsSuccess, run.StatusCode, run.ResponseTimeMs, run.ErrorMessage))
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return Ok(monitors);
    }

    [HttpPost]
    public async Task<ActionResult<MonitorListItemResponse>> Create([FromBody] CreateMonitorRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.TargetUrl))
        {
            return BadRequest(new { message = "Name and TargetUrl are required." });
        }

        if (!Uri.TryCreate(request.TargetUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return BadRequest(new { message = "TargetUrl must be a valid HTTP/HTTPS URL." });
        }

        var method = NormalizeMethod(request.Method);
        if (method is null)
        {
            return BadRequest(new { message = "Method must be GET, HEAD, POST, PUT, PATCH, or DELETE." });
        }

        if (request.CheckIntervalSeconds < 30 || request.CheckIntervalSeconds > 3600)
        {
            return BadRequest(new { message = "CheckIntervalSeconds must be between 30 and 3600." });
        }

        var monitor = new MonitorEntity
        {
            OwnerUserId = userId.Value,
            Name = request.Name.Trim(),
            TargetUrl = request.TargetUrl.Trim(),
            Method = method,
            CheckIntervalSeconds = request.CheckIntervalSeconds,
            IsActive = request.IsActive
        };

        _dbContext.Monitors.Add(monitor);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = monitor.Id },
            new MonitorListItemResponse(monitor.Id, monitor.Name, monitor.TargetUrl, monitor.Method, monitor.CheckIntervalSeconds, monitor.IsActive, monitor.CreatedAtUtc, null));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MonitorListItemResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var monitor = await _dbContext.Monitors
            .Where(x => x.Id == id && x.OwnerUserId == userId)
            .Select(x => new MonitorListItemResponse(
                x.Id,
                x.Name,
                x.TargetUrl,
                x.Method,
                x.CheckIntervalSeconds,
                x.IsActive,
                x.CreatedAtUtc,
                x.CheckRuns
                    .OrderByDescending(run => run.ExecutedAtUtc)
                    .Select(run => new LatestCheckResponse(run.ExecutedAtUtc, run.IsSuccess, run.StatusCode, run.ResponseTimeMs, run.ErrorMessage))
                    .FirstOrDefault()))
            .FirstOrDefaultAsync(cancellationToken);

        if (monitor is null)
        {
            return NotFound();
        }

        return Ok(monitor);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MonitorListItemResponse>> Update(Guid id, [FromBody] UpdateMonitorRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var monitor = await _dbContext.Monitors.FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == userId, cancellationToken);
        if (monitor is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.TargetUrl))
        {
            return BadRequest(new { message = "Name and TargetUrl are required." });
        }

        if (!Uri.TryCreate(request.TargetUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return BadRequest(new { message = "TargetUrl must be a valid HTTP/HTTPS URL." });
        }

        var method = NormalizeMethod(request.Method);
        if (method is null)
        {
            return BadRequest(new { message = "Method must be GET, HEAD, POST, PUT, PATCH, or DELETE." });
        }

        if (request.CheckIntervalSeconds < 30 || request.CheckIntervalSeconds > 3600)
        {
            return BadRequest(new { message = "CheckIntervalSeconds must be between 30 and 3600." });
        }

        monitor.Name = request.Name.Trim();
        monitor.TargetUrl = request.TargetUrl.Trim();
        monitor.Method = method;
        monitor.CheckIntervalSeconds = request.CheckIntervalSeconds;
        monitor.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var latest = await _dbContext.MonitorCheckRuns
            .Where(x => x.MonitorId == monitor.Id)
            .OrderByDescending(x => x.ExecutedAtUtc)
            .Select(run => new LatestCheckResponse(run.ExecutedAtUtc, run.IsSuccess, run.StatusCode, run.ResponseTimeMs, run.ErrorMessage))
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new MonitorListItemResponse(monitor.Id, monitor.Name, monitor.TargetUrl, monitor.Method, monitor.CheckIntervalSeconds, monitor.IsActive, monitor.CreatedAtUtc, latest));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var monitor = await _dbContext.Monitors.FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == userId, cancellationToken);
        if (monitor is null)
        {
            return NotFound();
        }

        _dbContext.Monitors.Remove(monitor);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/run-check")]
    public async Task<ActionResult<MonitorCheckRunResponse>> RunCheck(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var monitor = await _dbContext.Monitors.FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == userId, cancellationToken);
        if (monitor is null)
        {
            return NotFound();
        }

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(20);

        var checkRun = new MonitorCheckRun
        {
            MonitorId = monitor.Id,
            ExecutedAtUtc = DateTime.UtcNow
        };

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var request = new HttpRequestMessage(new HttpMethod(monitor.Method), monitor.TargetUrl);
            using var response = await client.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            checkRun.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
            checkRun.StatusCode = (int)response.StatusCode;
            checkRun.IsSuccess = response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            checkRun.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
            checkRun.IsSuccess = false;
            checkRun.ErrorMessage = ex.Message;
        }

        _dbContext.MonitorCheckRuns.Add(checkRun);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new MonitorCheckRunResponse(checkRun.Id, checkRun.MonitorId, checkRun.ExecutedAtUtc, checkRun.IsSuccess, checkRun.StatusCode, checkRun.ResponseTimeMs, checkRun.ErrorMessage));
    }

    [HttpGet("{id:guid}/checks")]
    public async Task<ActionResult<IReadOnlyList<MonitorCheckRunResponse>>> GetChecks(Guid id, [FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var monitorExists = await _dbContext.Monitors.AnyAsync(x => x.Id == id && x.OwnerUserId == userId, cancellationToken);
        if (!monitorExists)
        {
            return NotFound();
        }

        limit = Math.Clamp(limit, 1, 200);

        var runs = await _dbContext.MonitorCheckRuns
            .Where(x => x.MonitorId == id)
            .OrderByDescending(x => x.ExecutedAtUtc)
            .Take(limit)
            .Select(x => new MonitorCheckRunResponse(x.Id, x.MonitorId, x.ExecutedAtUtc, x.IsSuccess, x.StatusCode, x.ResponseTimeMs, x.ErrorMessage))
            .ToListAsync(cancellationToken);

        return Ok(runs);
    }

    private Guid? GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }

    private static string? NormalizeMethod(string method)
    {
        if (string.IsNullOrWhiteSpace(method)) return null;

        var normalized = method.Trim().ToUpperInvariant();
        return normalized switch
        {
            "GET" or "HEAD" or "POST" or "PUT" or "PATCH" or "DELETE" => normalized,
            _ => null
        };
    }

    public sealed record CreateMonitorRequest(string Name, string TargetUrl, string Method, int CheckIntervalSeconds, bool IsActive);
    public sealed record UpdateMonitorRequest(string Name, string TargetUrl, string Method, int CheckIntervalSeconds, bool IsActive);

    public sealed record MonitorListItemResponse(
        Guid Id,
        string Name,
        string TargetUrl,
        string Method,
        int CheckIntervalSeconds,
        bool IsActive,
        DateTime CreatedAtUtc,
        LatestCheckResponse? LatestCheck);

    public sealed record LatestCheckResponse(DateTime ExecutedAtUtc, bool IsSuccess, int? StatusCode, int? ResponseTimeMs, string? ErrorMessage);

    public sealed record MonitorCheckRunResponse(
        Guid Id,
        Guid MonitorId,
        DateTime ExecutedAtUtc,
        bool IsSuccess,
        int? StatusCode,
        int? ResponseTimeMs,
        string? ErrorMessage);
}