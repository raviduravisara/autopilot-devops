using AutoPilot.Api.Security;

namespace AutoPilot.Api.Tests.Security;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_And_Verify_WithSamePassword_ReturnsTrue()
    {
        var password = "StrongPass123!";

        var hash = PasswordHasher.Hash(password);
        var verified = PasswordHasher.Verify(password, hash);

        Assert.True(verified);
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = PasswordHasher.Hash("StrongPass123!");

        var verified = PasswordHasher.Verify("WrongPass456!", hash);

        Assert.False(verified);
    }
}