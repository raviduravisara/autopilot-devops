using AutoPilot.Api.Data;
using AutoPilot.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserAccount>>> List(CancellationToken cancellationToken)
    {
        var users = await _dbContext.UserAccounts
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserAccount>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await _dbContext.UserAccounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<UserAccount>> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "FullName and Email are required." });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var existing = await _dbContext.UserAccounts.AnyAsync(x => x.Email == email, cancellationToken);
        if (existing)
        {
            return Conflict(new { message = "A user with this email already exists." });
        }

        var user = new UserAccount
        {
            FullName = request.FullName.Trim(),
            Email = email
        };

        _dbContext.UserAccounts.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    public sealed record CreateUserRequest(string FullName, string Email);
}