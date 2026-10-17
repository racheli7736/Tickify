using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Tickify.Core.Common;
using Tickify.Core.DTOs.Auth;
using Tickify.Core.DTOs.Categories;
using Tickify.Core.Entities;
using Tickify.Core.Exceptions;
using Tickify.Core.Interfaces.Repositories;
using Tickify.Core.Interfaces.Services;
using Tickify.Data;
using Tickify.Data.Repositories;
using Tickify.Service.Security;
using Tickify.Service.Services;
using Tickify.Tests.Helpers;

namespace Tickify.Tests.Integration;

/// <summary>
/// Regression tests for the "two simultaneous requests create the same e-mail / category name" bug.
/// Before the fix the loser of the race surfaced as HTTP 500; it must be a 409 (Conflict).
/// </summary>
[Trait("Category", "PostgreSQL")]
[Collection(IntegrationCollection.Name)]
public class DuplicateKeyTests
{
    private readonly DatabaseFixture _fixture;

    public DuplicateKeyTests(DatabaseFixture fixture) => _fixture = fixture;

    private static string UniqueEmail() => $"race-{Guid.NewGuid():N}@example.com";

    private static User NewUser(string email) => new()
    {
        FullName = "Race Test", Email = email, PasswordHash = "hash", CreatedAt = DateTime.UtcNow
    };

    // ---------- the data layer translates the PostgreSQL error ----------

    [Fact]
    public async Task UnitOfWork_DuplicateEmail_ThrowsDuplicateKeyException()
    {
        var email = UniqueEmail();
        await using var first = _fixture.CreateContext();
        await using var second = _fixture.CreateContext();
        first.Users.Add(NewUser(email));
        second.Users.Add(NewUser(email));

        await new UnitOfWork(first).SaveChangesAsync(CancellationToken.None);

        await Assert.ThrowsAsync<DuplicateKeyException>(() => new UnitOfWork(second).SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task UnitOfWork_CategoryNameDiffersOnlyByCase_ThrowsDuplicateKeyException()
    {
        var name = $"Cat-{Guid.NewGuid():N}";
        await using var first = _fixture.CreateContext();
        await using var second = _fixture.CreateContext();
        first.Categories.Add(new Category { Name = name.ToLowerInvariant() });
        second.Categories.Add(new Category { Name = name.ToUpperInvariant() });

        await new UnitOfWork(first).SaveChangesAsync(CancellationToken.None);

        await Assert.ThrowsAsync<DuplicateKeyException>(() => new UnitOfWork(second).SaveChangesAsync(CancellationToken.None));
    }

    // ---------- the services turn it into a Conflict (race forced deterministically) ----------

    [Fact]
    public async Task Register_SameEmailAtTheSameTime_OneSucceedsAndTheOtherGetsConflict()
    {
        var email = UniqueEmail();
        var rendezvous = new Rendezvous(2);   // both pass the "e-mail is free" check before either saves
        await using var ctx1 = _fixture.CreateContext();
        await using var ctx2 = _fixture.CreateContext();
        var auth1 = CreateAuthService(ctx1, rendezvous);
        var auth2 = CreateAuthService(ctx2, rendezvous);
        var request = new RegisterRequest { FullName = "Race Test", Email = email, Password = "secret12" };

        var results = await Task.WhenAll(
            auth1.RegisterAsync(request, CancellationToken.None),
            auth2.RegisterAsync(request, CancellationToken.None));

        Assert.Single(results, r => r.IsSuccess);
        var loser = Assert.Single(results, r => !r.IsSuccess);
        Assert.Equal(ErrorType.Conflict, loser.ErrorType);

        await using var verify = _fixture.CreateContext();
        Assert.Equal(1, await verify.Users.CountAsync(u => u.Email == email));
    }

    [Fact]
    public async Task CreateCategory_SameNameAtTheSameTime_OneSucceedsAndTheOtherGetsConflict()
    {
        var name = $"Race-{Guid.NewGuid():N}";
        var rendezvous = new Rendezvous(2);
        await using var ctx1 = _fixture.CreateContext();
        await using var ctx2 = _fixture.CreateContext();
        var service1 = CreateCategoryService(ctx1, rendezvous);
        var service2 = CreateCategoryService(ctx2, rendezvous);

        var results = await Task.WhenAll(
            service1.CreateAsync(new CategoryRequest { Name = name }, CancellationToken.None),
            service2.CreateAsync(new CategoryRequest { Name = name.ToUpperInvariant() }, CancellationToken.None));

        Assert.Single(results, r => r.IsSuccess);
        var loser = Assert.Single(results, r => !r.IsSuccess);
        Assert.Equal(ErrorType.Conflict, loser.ErrorType);
    }

    private static AuthService CreateAuthService(AppDbContext ctx, Rendezvous rendezvous)
    {
        var tokens = new Mock<ITokenService>();
        tokens.Setup(t => t.CreateToken(It.IsAny<User>())).Returns(new AuthResponse { Token = "jwt" });
        return new AuthService(
            new RendezvousUserRepository(new UserRepository(ctx), rendezvous),
            new UnitOfWork(ctx), new BCryptPasswordHasher(), tokens.Object, TestMapper.Create(),
            NullLogger<AuthService>.Instance);
    }

    private static CategoryService CreateCategoryService(AppDbContext ctx, Rendezvous rendezvous) =>
        new(new RendezvousCategoryRepository(new CategoryRepository(ctx), rendezvous),
            new UnitOfWork(ctx), TestMapper.Create(), NullLogger<CategoryService>.Instance);

    /// <summary>Real repository; the "is it free?" check waits until both callers have asked.</summary>
    private sealed class RendezvousUserRepository : IUserRepository
    {
        private readonly IUserRepository _inner;
        private readonly Rendezvous _rendezvous;

        public RendezvousUserRepository(IUserRepository inner, Rendezvous rendezvous)
        {
            _inner = inner;
            _rendezvous = rendezvous;
        }

        public async Task<bool> EmailExistsAsync(string email, CancellationToken ct)
        {
            var exists = await _inner.EmailExistsAsync(email, ct);
            await _rendezvous.ArriveAsync();
            return exists;
        }

        public Task<User?> GetByIdAsync(int id, CancellationToken ct) => _inner.GetByIdAsync(id, ct);
        public Task<User?> GetByEmailAsync(string email, CancellationToken ct) => _inner.GetByEmailAsync(email, ct);
        public Task AddAsync(User user, CancellationToken ct) => _inner.AddAsync(user, ct);
    }

    private sealed class RendezvousCategoryRepository : ICategoryRepository
    {
        private readonly ICategoryRepository _inner;
        private readonly Rendezvous _rendezvous;

        public RendezvousCategoryRepository(ICategoryRepository inner, Rendezvous rendezvous)
        {
            _inner = inner;
            _rendezvous = rendezvous;
        }

        public async Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct)
        {
            var exists = await _inner.NameExistsAsync(name, excludeId, ct);
            await _rendezvous.ArriveAsync();
            return exists;
        }

        public Task<List<Category>> GetAllAsync(CancellationToken ct) => _inner.GetAllAsync(ct);
        public Task<Category?> GetByIdAsync(int id, CancellationToken ct) => _inner.GetByIdAsync(id, ct);
        public Task<Category?> GetByIdReadOnlyAsync(int id, CancellationToken ct) => _inner.GetByIdReadOnlyAsync(id, ct);
        public Task<List<Category>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct) => _inner.GetByIdsAsync(ids, ct);
        public Task<bool> HasEventsAsync(int id, CancellationToken ct) => _inner.HasEventsAsync(id, ct);
        public Task AddAsync(Category category, CancellationToken ct) => _inner.AddAsync(category, ct);
        public void Remove(Category category) => _inner.Remove(category);
    }
}
