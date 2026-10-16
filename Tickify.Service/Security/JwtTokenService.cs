using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tickify.Core.DTOs.Auth;
using Tickify.Core.Entities;
using Tickify.Core.Interfaces.Services;
using Tickify.Service.Options;

namespace Tickify.Service.Security;

public class JwtTokenService : ITokenService
{
    private readonly JwtSettings _settings;
    private readonly SigningCredentials _credentials;

    public JwtTokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
        _credentials = new SigningCredentials(JwtTokenValidation.CreateSigningKey(_settings), SecurityAlgorithms.HmacSha256);
    }

    public AuthResponse CreateToken(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: _credentials);

        return new AuthResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt,
            FullName = user.FullName,
            Role = user.Role
        };
    }
}
