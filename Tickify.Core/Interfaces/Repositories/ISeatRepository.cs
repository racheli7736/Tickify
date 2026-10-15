using Tickify.Core.Entities;

namespace Tickify.Core.Interfaces.Repositories;

public interface ISeatRepository
{
    Task<List<Seat>> GetByEventAsync(int eventId, CancellationToken ct);
    Task<List<Seat>> GetByIdsForUpdateAsync(int eventId, IEnumerable<int> ids, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<Seat> seats, CancellationToken ct);
}
