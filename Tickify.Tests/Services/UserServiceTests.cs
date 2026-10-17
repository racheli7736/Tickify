using Moq;
using Tickify.Core.Common;
using Tickify.Core.Entities;
using Tickify.Core.Enums;
using Tickify.Core.Interfaces.Repositories;
using Tickify.Service.Services;
using Tickify.Tests.Helpers;

namespace Tickify.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _users = new();

    private UserService Sut() => new(_users.Object, TestMapper.Create());

    private void GivenUser(int id) =>
        _users.Setup(u => u.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(new User
        {
            Id = id, FullName = "Dana Cohen", Email = "dana@example.com", PasswordHash = "SECRET-HASH", Role = UserRole.User,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

    [Fact]
    public async Task GetByIdAsync_OwnProfile_ReturnsNameAndRoleOnly()
    {
        GivenUser(7);

        var result = await Sut().GetByIdAsync(callerId: 7, isAdmin: false, userId: 7, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal((7, "Dana Cohen", UserRole.User), (result.Value!.Id, result.Value.FullName, result.Value.Role));
        // No e-mail and nothing credential-related can leave through the DTO.
        Assert.DoesNotContain(typeof(Core.DTOs.Users.UserResponse).GetProperties(),
            p => p.Name.Contains("Email", StringComparison.OrdinalIgnoreCase) || p.Name.Contains("Password", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetByIdAsync_AdminCanReadAnyProfile()
    {
        GivenUser(7);

        var result = await Sut().GetByIdAsync(callerId: 1, isAdmin: true, userId: 7, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetByIdAsync_AnotherUser_IsForbidden_WithoutTouchingTheDatabase()
    {
        var result = await Sut().GetByIdAsync(callerId: 8, isAdmin: false, userId: 7, CancellationToken.None);

        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        _users.VerifyNoOtherCalls();   // a stranger cannot even learn whether the id exists
    }

    [Fact]
    public async Task GetByIdAsync_UnknownUserAsAdmin_ReturnsNotFound()
    {
        _users.Setup(u => u.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await Sut().GetByIdAsync(callerId: 1, isAdmin: true, userId: 99, CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
