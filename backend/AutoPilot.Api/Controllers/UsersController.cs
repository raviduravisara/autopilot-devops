using AutoPilot.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AutoPilot.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public UsersController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserAccountView>>> List(CancellationToken cancellationToken)
    {
        var users = await _dbContext.UserAccounts
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new UserAccountView(x.Id, x.FullName, x.Email, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserAccountView>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await _dbContext.UserAccounts
            .Where(x => x.Id == id)
            .Select(x => new UserAccountView(x.Id, x.FullName, x.Email, x.CreatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserAccountView>> Me(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var user = await _dbContext.UserAccounts
            .Where(x => x.Id == userId)
            .Select(x => new UserAccountView(x.Id, x.FullName, x.Email, x.CreatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    public sealed record UserAccountView(Guid Id, string FullName, string Email, DateTime CreatedAtUtc);
}