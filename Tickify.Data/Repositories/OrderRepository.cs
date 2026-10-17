using Microsoft.EntityFrameworkCore;
using Tickify.Core.DTOs.Common;
using Tickify.Core.DTOs.Orders;
using Tickify.Core.Entities;
using Tickify.Core.Interfaces.Repositories;

namespace Tickify.Data.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;

    public OrderRepository(AppDbContext db) => _db = db;

    public async Task<PagedResult<Order>> GetPagedAsync(int? userId, OrderQueryParameters query, CancellationToken ct)
    {
        IQueryable<Order> orders = _db.Orders.AsNoTracking();

        if (userId is int id)
            orders = orders.Where(o => o.UserId == id);

        var totalCount = await orders.CountAsync(ct);

        var items = await orders
            .Include(o => o.Event)
            .Include(o => o.User)
            .OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<Order>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public Task<Order?> GetByIdWithDetailsAsync(int id, CancellationToken ct) =>
        _db.Orders.AsNoTracking()
            .Include(o => o.Event)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    // Tracked: CancelAsync modifies the order and releases its seats.
    public Task<Order?> GetByIdWithSeatsAsync(int id, CancellationToken ct) =>
        _db.Orders
            .Include(o => o.Seats)
            .Include(o => o.Event)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task AddAsync(Order order, CancellationToken ct) =>
        await _db.Orders.AddAsync(order, ct);
}
