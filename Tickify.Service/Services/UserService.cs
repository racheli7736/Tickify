using AutoMapper;
using Tickify.Core.Common;
using Tickify.Core.DTOs.Users;
using Tickify.Core.Interfaces.Repositories;
using Tickify.Core.Interfaces.Services;

namespace Tickify.Service.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IMapper _mapper;

    public UserService(IUserRepository users, IMapper mapper)
    {
        _users = users;
        _mapper = mapper;
    }

    public async Task<ServiceResult<UserResponse>> GetByIdAsync(int callerId, bool isAdmin, int userId, CancellationToken ct)
    {
        // Ownership first, so a caller cannot probe which user ids exist.
        if (!isAdmin && callerId != userId)
            return ServiceResult<UserResponse>.Fail(ErrorType.Forbidden, "You can only view your own profile.");

        var user = await _users.GetByIdAsync(userId, ct);
        return user is null
            ? ServiceResult<UserResponse>.Fail(ErrorType.NotFound, "User not found.")
            : ServiceResult<UserResponse>.Success(_mapper.Map<UserResponse>(user));
    }
}
