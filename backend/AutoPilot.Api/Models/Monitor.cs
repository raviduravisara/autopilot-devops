namespace AutoPilot.Api.Models;

public class Monitor
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public int CheckIntervalSeconds { get; set; } = 60;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LastCheckedAtUtc { get; set; }
    public bool? LastCheckSucceeded { get; set; }
    public int? LastStatusCode { get; set; }
    public int? LastResponseTimeMs { get; set; }
    public string? LastErrorMessage { get; set; }
    public int ConsecutiveSuccessCount { get; set; }
    public int ConsecutiveFailureCount { get; set; }

    public UserAccount? OwnerUser { get; set; }
    public ICollection<MonitorCheckRun> CheckRuns { get; set; } = new List<MonitorCheckRun>();
}