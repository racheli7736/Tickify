using System.Security.Claims;
using Tickify.Core.Enums;

namespace Tickify.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException("The authenticated user has no valid id claim.");
    }

    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(nameof(UserRole.Admin));
}
