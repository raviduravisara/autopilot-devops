using AutoPilot.Api.Data;
using AutoPilot.Api.Services.Monitoring;
using Microsoft.EntityFrameworkCore;

namespace AutoPilot.Api.Background;

public sealed class MonitorSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MonitorSchedulerService> _logger;

    public MonitorSchedulerService(IServiceScopeFactory scopeFactory, ILogger<MonitorSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var runner = scope.ServiceProvider.GetRequiredService<IMonitorCheckRunner>();

                var now = DateTime.UtcNow;
                var candidates = await dbContext.Monitors
                    .Where(x => x.IsActive)
                    .Where(x => x.LastCheckedAtUtc == null || x.LastCheckedAtUtc <= now.AddSeconds(-x.CheckIntervalSeconds))
                    .OrderBy(x => x.LastCheckedAtUtc)
                    .Take(50)
                    .Select(x => x.Id)
                    .ToListAsync(stoppingToken);

                foreach (var monitorId in candidates)
                {
                    await runner.RunCheckAsync(monitorId, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while executing scheduled monitor checks.");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
}