using System.Text;
using Microsoft.IdentityModel.Tokens;
using Tickify.Service.Options;

namespace Tickify.Service.Security;

/// <summary>Single source of truth for the signing key and validation rules, shared by token creation and the API.</summary>
public static class JwtTokenValidation
{
    public static SymmetricSecurityKey CreateSigningKey(JwtSettings settings) =>
        new(Encoding.UTF8.GetBytes(settings.Key));

    public static TokenValidationParameters CreateParameters(JwtSettings settings) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = settings.Issuer,
        ValidateAudience = true,
        ValidAudience = settings.Audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = CreateSigningKey(settings),
        ClockSkew = TimeSpan.Zero
    };
}
