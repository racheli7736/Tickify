using Microsoft.EntityFrameworkCore;
using Tickify.Core.DTOs.Common;
using Tickify.Core.DTOs.Events;
using Tickify.Core.Entities;
using Tickify.Core.Enums;
using Tickify.Core.Interfaces.Repositories;

namespace Tickify.Data.Repositories;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _db;

    public EventRepository(AppDbContext db) => _db = db;

    public async Task<PagedResult<Event>> GetPagedAsync(EventQueryParameters query, bool includePast, CancellationToken ct)
    {
        IQueryable<Event> events = _db.Events.AsNoTracking();

        if (!includePast)
        {
            var now = DateTime.UtcNow;
            events = events.Where(e => e.StartsAt > now);
        }

        if (query.CategoryId is int categoryId)
            events = events.Where(e => e.EventCategories.Any(ec => ec.CategoryId == categoryId));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            events = events.Where(e => e.Name.Contains(search));
        }

        var descending = string.Equals(query.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        events = (query.SortBy?.ToLowerInvariant()) switch
        {
            "price" => descending ? events.OrderByDescending(e => e.TicketPrice).ThenBy(e => e.Id)
                                  : events.OrderBy(e => e.TicketPrice).ThenBy(e => e.Id),
            "name" => descending ? events.OrderByDescending(e => e.Name).ThenBy(e => e.Id)
                                 : events.OrderBy(e => e.Name).ThenBy(e => e.Id),
            _ => descending ? events.OrderByDescending(e => e.StartsAt).ThenBy(e => e.Id)
                            : events.OrderBy(e => e.StartsAt).ThenBy(e => e.Id),
        };

        var totalCount = await events.CountAsync(ct);

        // Skip/Take run inside the SQL query; only one page is ever materialized.
        var items = await events
            .Include(e => e.EventCategories).ThenInclude(ec => ec.Category)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<Event>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public Task<Event?> GetByIdWithDetailsAsync(int id, CancellationToken ct) =>
        _db.Events.AsNoTracking()
            .Include(e => e.EventCategories).ThenInclude(ec => ec.Category)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<Event?> GetByIdForUpdateAsync(int id, CancellationToken ct) =>
        _db.Events
            .Include(e => e.EventCategories)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<bool> HasSoldSeatsAsync(int id, CancellationToken ct) =>
        _db.Seats.AsNoTracking().AnyAsync(s => s.EventId == id && s.Status == SeatStatus.Sold, ct);

    public Task<bool> HasOrdersAsync(int id, CancellationToken ct) =>
        _db.Orders.AsNoTracking().AnyAsync(o => o.EventId == id, ct);

    public async Task AddAsync(Event ev, CancellationToken ct) =>
        await _db.Events.AddAsync(ev, ct);

    public void Remove(Event ev) => _db.Events.Remove(ev);
}
