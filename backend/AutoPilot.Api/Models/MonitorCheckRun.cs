namespace AutoPilot.Api.Models;

public class MonitorCheckRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MonitorId { get; set; }
    public DateTime ExecutedAtUtc { get; set; } = DateTime.UtcNow;
    public int? ResponseTimeMs { get; set; }
    public int? StatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }

    public Monitor? Monitor { get; set; }
}