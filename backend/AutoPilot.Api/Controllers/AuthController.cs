using AutoPilot.Api.Data;
using AutoPilot.Api.Models;
using AutoPilot.Api.Security;
using AutoPilot.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPilot.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IJwtTokenService _tokenService;

    public AuthController(AppDbContext dbContext, IJwtTokenService tokenService)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "FullName, Email and Password are required." });
        }

        if (request.Password.Length < 8)
        {
            return BadRequest(new { message = "Password must be at least 8 characters." });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var exists = await _dbContext.UserAccounts.AnyAsync(x => x.Email == email, cancellationToken);
        if (exists)
        {
            return Conflict(new { message = "Email is already registered." });
        }

        var role = AppRole.Patient;
        if (!string.IsNullOrWhiteSpace(request.Role) && Enum.TryParse<AppRole>(request.Role, true, out var parsedRole))
        {
            role = parsedRole;
        }

        var user = new UserAccount
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = role
        };

        _dbContext.UserAccounts.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var token = _tokenService.CreateToken(user);
        return Ok(new AuthResponse(token, new UserView(user.Id, user.FullName, user.Email, user.Role.ToString(), user.CreatedAtUtc)));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email and Password are required." });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.UserAccounts.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var token = _tokenService.CreateToken(user);
        return Ok(new AuthResponse(token, new UserView(user.Id, user.FullName, user.Email, user.Role.ToString(), user.CreatedAtUtc)));
    }

    public sealed record RegisterRequest(string FullName, string Email, string Password, string? Role);
    public sealed record LoginRequest(string Email, string Password);
    public sealed record AuthResponse(string AccessToken, UserView User);
    public sealed record UserView(Guid Id, string FullName, string Email, string Role, DateTime CreatedAtUtc);
}