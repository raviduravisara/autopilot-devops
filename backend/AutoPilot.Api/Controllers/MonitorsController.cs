using System.Security.Claims;
using AutoPilot.Api.Data;
using AutoPilot.Api.Models;
using AutoPilot.Api.Services.Monitoring;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MonitorEntity = AutoPilot.Api.Models.Monitor;

namespace AutoPilot.Api.Controllers;

[ApiController]
[Route("api/monitors")]
[Authorize]
public class MonitorsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IMonitorCheckRunner _monitorCheckRunner;

    public MonitorsController(AppDbContext dbContext, IMonitorCheckRunner monitorCheckRunner)
    {
        _dbContext = dbContext;
        _monitorCheckRunner = monitorCheckRunner;
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
                x.LastCheckedAtUtc,
                x.LastCheckSucceeded,
                x.LastStatusCode,
                x.LastResponseTimeMs,
                x.LastErrorMessage,
                x.ConsecutiveSuccessCount,
                x.ConsecutiveFailureCount))
            .ToListAsync(cancellationToken);

        return Ok(monitors);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<MonitorsSummaryResponse>> Summary(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var monitors = await _dbContext.Monitors
            .Where(x => x.OwnerUserId == userId)
            .Select(x => new { x.IsActive, x.LastCheckSucceeded })
            .ToListAsync(cancellationToken);

        var total = monitors.Count;
        var paused = monitors.Count(x => !x.IsActive);
        var up = monitors.Count(x => x.IsActive && x.LastCheckSucceeded == true);
        var down = monitors.Count(x => x.IsActive && x.LastCheckSucceeded == false);
        var unknown = monitors.Count(x => x.IsActive && x.LastCheckSucceeded == null);

        return Ok(new MonitorsSummaryResponse(total, up, down, paused, unknown));
    }

    [HttpGet("recent-checks")]
    public async Task<ActionResult<IReadOnlyList<RecentCheckResponse>>> RecentChecks([FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        limit = Math.Clamp(limit, 1, 200);

        var checks = await _dbContext.MonitorCheckRuns
            .Where(x => x.Monitor != null && x.Monitor.OwnerUserId == userId)
            .OrderByDescending(x => x.ExecutedAtUtc)
            .Take(limit)
            .Select(x => new RecentCheckResponse(
                x.Id,
                x.MonitorId,
                x.Monitor!.Name,
                x.ExecutedAtUtc,
                x.IsSuccess,
                x.StatusCode,
                x.ResponseTimeMs,
                x.ErrorMessage))
            .ToListAsync(cancellationToken);

        return Ok(checks);
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

        return CreatedAtAction(nameof(GetById), new { id = monitor.Id }, ToResponse(monitor));
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
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == userId, cancellationToken);

        if (monitor is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(monitor));
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

        return Ok(ToResponse(monitor));
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

    [EnableRateLimiting("run-check")]
    [HttpPost("{id:guid}/run-check")]
    public async Task<ActionResult<MonitorCheckRunResponse>> RunCheck(Guid id, CancellationToken cancellationToken)
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

        var run = await _monitorCheckRunner.RunCheckAsync(id, cancellationToken);
        if (run is null)
        {
            return NotFound();
        }

        return Ok(new MonitorCheckRunResponse(run.Id, run.MonitorId, run.ExecutedAtUtc, run.IsSuccess, run.StatusCode, run.ResponseTimeMs, run.ErrorMessage));
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

    private static MonitorListItemResponse ToResponse(MonitorEntity monitor)
    {
        return new MonitorListItemResponse(
            monitor.Id,
            monitor.Name,
            monitor.TargetUrl,
            monitor.Method,
            monitor.CheckIntervalSeconds,
            monitor.IsActive,
            monitor.CreatedAtUtc,
            monitor.LastCheckedAtUtc,
            monitor.LastCheckSucceeded,
            monitor.LastStatusCode,
            monitor.LastResponseTimeMs,
            monitor.LastErrorMessage,
            monitor.ConsecutiveSuccessCount,
            monitor.ConsecutiveFailureCount);
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
        DateTime? LastCheckedAtUtc,
        bool? LastCheckSucceeded,
        int? LastStatusCode,
        int? LastResponseTimeMs,
        string? LastErrorMessage,
        int ConsecutiveSuccessCount,
        int ConsecutiveFailureCount);

    public sealed record MonitorCheckRunResponse(
        Guid Id,
        Guid MonitorId,
        DateTime ExecutedAtUtc,
        bool IsSuccess,
        int? StatusCode,
        int? ResponseTimeMs,
        string? ErrorMessage);

    public sealed record MonitorsSummaryResponse(int Total, int Up, int Down, int Paused, int Unknown);

    public sealed record RecentCheckResponse(
        Guid Id,
        Guid MonitorId,
        string MonitorName,
        DateTime ExecutedAtUtc,
        bool IsSuccess,
        int? StatusCode,
        int? ResponseTimeMs,
        string? ErrorMessage);
}

