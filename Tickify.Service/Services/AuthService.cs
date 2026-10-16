using Microsoft.Extensions.Logging;
using Tickify.Core.Common;
using Tickify.Core.DTOs.Auth;
using Tickify.Core.Entities;
using Tickify.Core.Enums;
using Tickify.Core.Interfaces.Repositories;
using Tickify.Core.Interfaces.Services;
using Tickify.Service.Mapping;

namespace Tickify.Service.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IUnitOfWork unitOfWork,
        IPasswordHasher hasher,
        ITokenService tokens,
        ILogger<AuthService> logger)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _hasher = hasher;
        _tokens = tokens;
        _logger = logger;
    }

    public async Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _users.EmailExistsAsync(email, ct))
            return ServiceResult<AuthResponse>.Fail(ErrorType.Conflict, "A user with this email already exists.");

        var user = request.ToEntity();
        user.Email = email;
        user.PasswordHash = _hasher.Hash(request.Password);
        user.Role = UserRole.User;
        user.CreatedAt = DateTime.UtcNow;

        await _users.AddAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} registered", user.Id);
        return ServiceResult<AuthResponse>.Success(_tokens.CreateToken(user));
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _users.GetByEmailAsync(email, ct);

        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
        {
            // Same answer for unknown email and wrong password; never log the password.
            _logger.LogWarning("Failed login attempt for {Email}", email);
            return ServiceResult<AuthResponse>.Fail(ErrorType.Unauthorized, "Invalid email or password.");
        }

        _logger.LogInformation("User {UserId} logged in", user.Id);
        return ServiceResult<AuthResponse>.Success(_tokens.CreateToken(user));
    }
}
