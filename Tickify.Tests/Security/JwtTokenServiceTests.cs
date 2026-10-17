using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tickify.Core.Entities;
using Tickify.Core.Enums;
using Tickify.Service.Options;
using Tickify.Service.Security;

namespace Tickify.Tests.Security;

public class JwtTokenServiceTests
{
    private static JwtSettings Settings(string? key = null) => new()
    {
        Issuer = "Tickify",
        Audience = "Tickify.Client",
        ExpiryMinutes = 30,
        Key = key ?? new string('k', 64)
    };

    private static User Admin() => new() { Id = 42, FullName = "Admin", Email = "admin@tickets.local", Role = UserRole.Admin };

    private static string CreateToken(JwtSettings settings, User user) =>
        new JwtTokenService(Options.Create(settings)).CreateToken(user).Token;

    private static ClaimsPrincipal Validate(string token, JwtSettings settings) =>
        new JwtSecurityTokenHandler().ValidateToken(token, JwtTokenValidation.CreateParameters(settings), out _);

    [Fact]
    public void CreateToken_ReturnsResponseWithUserDetailsAndExpiry()
    {
        var response = new JwtTokenService(Options.Create(Settings())).CreateToken(Admin());

        Assert.Equal(42, response.UserId);
        Assert.Equal("Admin", response.FullName);
        Assert.Equal(UserRole.Admin, response.Role);
        Assert.InRange(response.ExpiresAt, DateTime.UtcNow.AddMinutes(29), DateTime.UtcNow.AddMinutes(31));
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
    }

    [Fact]
    public void CreatedToken_ContainsIdEmailNameAndRoleClaims()
    {
        var settings = Settings();

        var principal = Validate(CreateToken(settings, Admin()), settings);

        Assert.Equal("42", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("admin@tickets.local", principal.FindFirst(ClaimTypes.Email)?.Value);
        Assert.Equal("Admin", principal.FindFirst(ClaimTypes.Name)?.Value);
        Assert.True(principal.IsInRole("Admin"));
    }

    [Fact]
    public void Validation_RejectsTokenSignedWithAnotherKey()
    {
        var token = CreateToken(Settings(new string('a', 64)), Admin());

        Assert.ThrowsAny<SecurityTokenException>(() => Validate(token, Settings(new string('b', 64))));
    }

    [Fact]
    public void Validation_RejectsTokenForAnotherAudience()
    {
        var token = CreateToken(Settings(), Admin());
        var other = Settings();
        other.Audience = "Someone.Else";

        Assert.ThrowsAny<SecurityTokenException>(() => Validate(token, other));
    }

    [Fact]
    public void Validation_RejectsExpiredToken()
    {
        var settings = Settings();
        var expired = new JwtSecurityToken(
            settings.Issuer, settings.Audience, new[] { new Claim(JwtRegisteredClaimNames.Sub, "1") },
            notBefore: DateTime.UtcNow.AddMinutes(-10), expires: DateTime.UtcNow.AddMinutes(-5),
            signingCredentials: new SigningCredentials(JwtTokenValidation.CreateSigningKey(settings), SecurityAlgorithms.HmacSha256));

        var token = new JwtSecurityTokenHandler().WriteToken(expired);

        Assert.ThrowsAny<SecurityTokenException>(() => Validate(token, settings));
    }
}
