using Tickify.Core.DTOs.Auth;
using Tickify.Core.Entities;

namespace Tickify.Core.Interfaces.Services;

public interface ITokenService
{
    AuthResponse CreateToken(User user);
}
