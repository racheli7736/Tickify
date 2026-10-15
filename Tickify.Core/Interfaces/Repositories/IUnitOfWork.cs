namespace Tickify.Core.Interfaces.Repositories;

public interface IUnitOfWork
{
    /// <exception cref="Exceptions.ConcurrencyConflictException">A row was changed by someone else since it was read.</exception>
    Task<int> SaveChangesAsync(CancellationToken ct);
}
