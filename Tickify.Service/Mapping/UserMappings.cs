using Tickify.Core.DTOs.Auth;
using Tickify.Core.Entities;

namespace Tickify.Service.Mapping;

public static class UserMappings
{
    // PasswordHash is set by AuthService after hashing; the role is always User on registration.
    public static User ToEntity(this RegisterRequest request) => new()
    {
        FullName = request.FullName,
        Email = request.Email
    };
}
