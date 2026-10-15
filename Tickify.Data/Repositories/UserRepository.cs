using Microsoft.EntityFrameworkCore;
using Tickify.Core.Entities;
using Tickify.Core.Interfaces.Repositories;

namespace Tickify.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db) => _db = db;

    // Tracked: the caller may need to update the user.
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
        _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct) =>
        _db.Users.AsNoTracking().AnyAsync(u => u.Email == email, ct);

    public async Task AddAsync(User user, CancellationToken ct) =>
        await _db.Users.AddAsync(user, ct);
}
