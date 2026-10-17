using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tickify.Core.Exceptions;
using Tickify.Core.Interfaces.Repositories;

namespace Tickify.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    public UnitOfWork(AppDbContext db) => _db = db;

    public async Task<int> SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            return await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Keep the service layer free of any EF Core dependency.
            throw new ConcurrencyConflictException("A row was modified by another request.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // A unique index rejected the row: the service decides which 409 message fits.
            throw new DuplicateKeyException("A unique constraint was violated.", ex);
        }
    }
}
