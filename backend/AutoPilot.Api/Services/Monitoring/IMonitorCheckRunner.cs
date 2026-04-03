using AutoPilot.Api.Models;

namespace AutoPilot.Api.Services.Monitoring;

public interface IMonitorCheckRunner
{
    Task<MonitorCheckRun?> RunCheckAsync(Guid monitorId, CancellationToken cancellationToken);
}