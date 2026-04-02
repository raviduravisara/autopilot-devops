using AutoPilot.Api.Models;

namespace AutoPilot.Api.Services;

public interface IJwtTokenService
{
    string CreateToken(UserAccount user);
}