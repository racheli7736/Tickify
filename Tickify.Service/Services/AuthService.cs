using AutoMapper;
using Microsoft.Extensions.Logging;
using Tickify.Core.Common;
using Tickify.Core.DTOs.Auth;
using Tickify.Core.Entities;
using Tickify.Core.Enums;
using Tickify.Core.Exceptions;
using Tickify.Core.Interfaces.Repositories;
using Tickify.Core.Interfaces.Services;

namespace Tickify.Service.Services;

public class AuthService : IAuthService
{
    private const string EmailTakenMessage = "A user with this email already exists.";

    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IUnitOfWork unitOfWork,
        IPasswordHasher hasher,
        ITokenService tokens,
        IMapper mapper,
        ILogger<AuthService> logger)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _hasher = hasher;
        _tokens = tokens;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _users.EmailExistsAsync(email, ct))
            return ServiceResult<AuthResponse>.Fail(ErrorType.Conflict, EmailTakenMessage);

        var user = _mapper.Map<User>(request);
        user.Email = email;
        user.PasswordHash = _hasher.Hash(request.Password);
        user.Role = UserRole.User;
        user.CreatedAt = DateTime.UtcNow;

        await _users.AddAsync(user, ct);
        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DuplicateKeyException)
        {
            // Another request registered the same e-mail between our check and our save.
            _logger.LogWarning("Registration lost a race for an e-mail that was just taken");
            return ServiceResult<AuthResponse>.Fail(ErrorType.Conflict, EmailTakenMessage);
        }

        _logger.LogInformation("User {UserId} registered", user.Id);
        return ServiceResult<AuthResponse>.Success(_tokens.CreateToken(user));
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _users.GetByEmailAsync(email, ct);

        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
        {
            // Same answer for unknown email and wrong password. Neither the password nor the email is logged;
            // the correlation id ties this line to the request log.
            _logger.LogWarning("Failed login attempt");
            return ServiceResult<AuthResponse>.Fail(ErrorType.Unauthorized, "Invalid email or password.");
        }

        _logger.LogInformation("User {UserId} logged in", user.Id);
        return ServiceResult<AuthResponse>.Success(_tokens.CreateToken(user));
    }
}
