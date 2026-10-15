using Tickify.Core.Common;
using Tickify.Core.DTOs.Auth;

namespace Tickify.Core.Interfaces.Services;

public interface IAuthService
{
    Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct);
}
