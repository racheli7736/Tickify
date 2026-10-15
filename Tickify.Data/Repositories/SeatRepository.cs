using Microsoft.EntityFrameworkCore;
using Tickify.Core.Entities;
using Tickify.Core.Enums;
using Tickify.Core.Interfaces.Repositories;

namespace Tickify.Data.Repositories;

public class SeatRepository : ISeatRepository
{
    private readonly AppDbContext _db;

    public SeatRepository(AppDbContext db) => _db = db;

    public Task<List<Seat>> GetByEventAsync(int eventId, CancellationToken ct) =>
        _db.Seats.AsNoTracking()
            .Where(s => s.EventId == eventId)
            .OrderBy(s => s.Row).ThenBy(s => s.Number)
            .ToListAsync(ct);

    // Tracked on purpose: the purchase/cancel flows modify these seats and
    // EF needs the original RowVersion for the concurrency check.
    public Task<List<Seat>> GetByIdsForUpdateAsync(int eventId, IEnumerable<int> ids, CancellationToken ct)
    {
        var idList = ids.Distinct().ToList();
        return _db.Seats.Where(s => s.EventId == eventId && idList.Contains(s.Id)).ToListAsync(ct);
    }

    public async Task<Dictionary<int, int>> GetAvailableCountsAsync(IEnumerable<int> eventIds, CancellationToken ct)
    {
        var idList = eventIds.Distinct().ToList();
        return await _db.Seats.AsNoTracking()
            .Where(s => idList.Contains(s.EventId) && s.Status == SeatStatus.Available)
            .GroupBy(s => s.EventId)
            .Select(g => new { EventId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EventId, x => x.Count, ct);
    }

    public async Task<(int Rows, int SeatsPerRow)> GetLayoutAsync(int eventId, CancellationToken ct)
    {
        var layout = await _db.Seats.AsNoTracking()
            .Where(s => s.EventId == eventId)
            .GroupBy(s => 1)
            .Select(g => new { Rows = g.Max(s => s.Row), SeatsPerRow = g.Max(s => s.Number) })
            .FirstOrDefaultAsync(ct);

        return layout is null ? (0, 0) : (layout.Rows, layout.SeatsPerRow);
    }

    public Task AddRangeAsync(IEnumerable<Seat> seats, CancellationToken ct) =>
        _db.Seats.AddRangeAsync(seats, ct);
}
