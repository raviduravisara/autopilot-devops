using System.IdentityModel.Tokens.Jwt;
using AutoPilot.Api.Config;
using AutoPilot.Api.Models;
using AutoPilot.Api.Services;
using Microsoft.Extensions.Options;

namespace AutoPilot.Api.Tests.Services;

public class JwtTokenServiceTests
{
    [Fact]
    public void CreateToken_ProducesTokenWithExpectedClaims()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "autopilot-test",
            Audience = "autopilot-client",
            SigningKey = "test_signing_key_value_with_more_than_32_chars",
            ExpiresMinutes = 30
        });

        var service = new JwtTokenService(options);
        var user = new UserAccount
        {
            Id = Guid.NewGuid(),
            FullName = "Ravidu",
            Email = "ravidu@example.com",
            Role = AppRole.Admin,
            PasswordHash = "ignored"
        };

        var token = service.CreateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("autopilot-test", jwt.Issuer);
        Assert.Contains(jwt.Audiences, audience => audience == "autopilot-client");
        Assert.Contains(jwt.Claims, claim => claim.Type == JwtRegisteredClaimNames.Email && claim.Value == user.Email);
    }
}