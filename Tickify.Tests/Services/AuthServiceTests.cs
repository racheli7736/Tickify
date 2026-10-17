using Microsoft.Extensions.Logging;
using Moq;
using Tickify.Core.Common;
using Tickify.Core.DTOs.Auth;
using Tickify.Core.Entities;
using Tickify.Core.Enums;
using Tickify.Core.Exceptions;
using Tickify.Core.Interfaces.Repositories;
using Tickify.Core.Interfaces.Services;
using Tickify.Service.Services;
using Tickify.Tests.Helpers;

namespace Tickify.Tests.Services;

public class AuthServiceTests
{
    private const string Email = "Someone@Example.com";
    private const string Password = "S3cret-pass";

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly ListLogger<AuthService> _logger = new();

    public AuthServiceTests()
    {
        _tokens.Setup(t => t.CreateToken(It.IsAny<User>()))
            .Returns((User u) => new AuthResponse { Token = "jwt", FullName = u.FullName, Role = u.Role });
    }

    private AuthService Sut() => new(_users.Object, _unitOfWork.Object, _hasher.Object, _tokens.Object, TestMapper.Create(), _logger);

    private static RegisterRequest Register() => new() { FullName = "Some One", Email = $"  {Email}  ", Password = Password };

    // ---------- RegisterAsync ----------

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ReturnsConflictWithoutCreatingAUser()
    {
        _users.Setup(u => u.EmailExistsAsync("someone@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await Sut().RegisterAsync(Register(), CancellationToken.None);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _users.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_NewEmail_CreatesUserWithRoleUserAndHashedPassword()
    {
        User? added = null;
        _users.Setup(u => u.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _users.Setup(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => added = u).Returns(Task.CompletedTask);
        _hasher.Setup(h => h.Hash(Password)).Returns("HASHED");

        var result = await Sut().RegisterAsync(Register(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("jwt", result.Value!.Token);
        Assert.Equal("someone@example.com", added!.Email);          // trimmed and lower-cased
        Assert.Equal(UserRole.User, added.Role);                    // registration never creates admins
        Assert.Equal("HASHED", added.PasswordHash);                 // the plain password is never stored
        Assert.NotEqual(Password, added.PasswordHash);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_DoesNotLogThePasswordOrTheEmail()
    {
        _users.Setup(u => u.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _hasher.Setup(h => h.Hash(Password)).Returns("HASHED");

        await Sut().RegisterAsync(Register(), CancellationToken.None);

        Assert.All(_logger.Entries, e =>
        {
            Assert.DoesNotContain(Password, e.Message);
            Assert.DoesNotContain("example.com", e.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("jwt", e.Message);
        });
    }

    // ---------- LoginAsync ----------

    private static User ExistingUser() => new()
    {
        Id = 3, FullName = "Some One", Email = "someone@example.com", PasswordHash = "HASHED", Role = UserRole.User
    };

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        var user = ExistingUser();
        _users.Setup(u => u.GetByEmailAsync("someone@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify(Password, "HASHED")).Returns(true);

        var result = await Sut().LoginAsync(new LoginRequest { Email = Email, Password = Password }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("jwt", result.Value!.Token);
        _tokens.Verify(t => t.CreateToken(user), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsUnauthorizedAndLogsWarningWithoutSecrets()
    {
        _users.Setup(u => u.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(ExistingUser());
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var result = await Sut().LoginAsync(new LoginRequest { Email = Email, Password = "wrong-password" }, CancellationToken.None);

        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
        _tokens.Verify(t => t.CreateToken(It.IsAny<User>()), Times.Never);
        var warning = Assert.Single(_logger.Messages(LogLevel.Warning));
        Assert.DoesNotContain("wrong-password", warning);
        Assert.DoesNotContain("example.com", warning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ReturnsTheSameUnauthorizedAnswer()
    {
        _users.Setup(u => u.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await Sut().LoginAsync(new LoginRequest { Email = Email, Password = Password }, CancellationToken.None);

        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
        Assert.Equal("Invalid email or password.", result.Error);   // no hint whether the email exists
        _hasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_EmailTakenBetweenCheckAndSave_ReturnsConflictInsteadOfFailing()
    {
        // Two requests passed the "e-mail is free" check; the unique index rejected the second save.
        _users.Setup(u => u.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _hasher.Setup(h => h.Hash(Password)).Returns("HASHED");
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new DuplicateKeyException("duplicate"));

        var result = await Sut().RegisterAsync(Register(), CancellationToken.None);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal("A user with this email already exists.", result.Error);
        _tokens.Verify(t => t.CreateToken(It.IsAny<User>()), Times.Never);
        var warning = Assert.Single(_logger.Messages(LogLevel.Warning));
        Assert.DoesNotContain("example.com", warning, StringComparison.OrdinalIgnoreCase);
    }
}
