using AutoPilot.Api.Data;
using AutoPilot.Api.Models;
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

    [Authorize(Policy = "ManageUsers")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserView>>> List(CancellationToken cancellationToken)
    {
        var users = await _dbContext.UserAccounts
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new UserView(x.Id, x.FullName, x.Email, x.Role.ToString(), x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [Authorize(Policy = "ManageUsers")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserView>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await _dbContext.UserAccounts
            .Where(x => x.Id == id)
            .Select(x => new UserView(x.Id, x.FullName, x.Email, x.Role.ToString(), x.CreatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserView>> Me(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var user = await _dbContext.UserAccounts
            .Where(x => x.Id == userId)
            .Select(x => new UserView(x.Id, x.FullName, x.Email, x.Role.ToString(), x.CreatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    public sealed record UserView(Guid Id, string FullName, string Email, string Role, DateTime CreatedAtUtc);
}