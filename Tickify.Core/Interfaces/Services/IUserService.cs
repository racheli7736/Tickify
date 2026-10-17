using Tickify.Core.Common;
using Tickify.Core.DTOs.Users;

namespace Tickify.Core.Interfaces.Services;

public interface IUserService
{
    /// <summary>A user may read only their own profile; an admin may read any.</summary>
    Task<ServiceResult<UserResponse>> GetByIdAsync(int callerId, bool isAdmin, int userId, CancellationToken ct);
}
