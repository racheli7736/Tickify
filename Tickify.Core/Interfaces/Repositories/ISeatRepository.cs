using Tickify.Core.Entities;

namespace Tickify.Core.Interfaces.Repositories;

public interface ISeatRepository
{
    Task<List<Seat>> GetByEventAsync(int eventId, CancellationToken ct);
    Task<List<Seat>> GetByIdsForUpdateAsync(int eventId, IEnumerable<int> ids, CancellationToken ct);

    /// <summary>Available seat count per event, in a single grouped query.</summary>
    Task<Dictionary<int, int>> GetAvailableCountsAsync(IEnumerable<int> eventIds, CancellationToken ct);

    /// <summary>Seat map dimensions of an event (0,0 when it has no seats).</summary>
    Task<(int Rows, int SeatsPerRow)> GetLayoutAsync(int eventId, CancellationToken ct);

    Task AddRangeAsync(IEnumerable<Seat> seats, CancellationToken ct);
}
