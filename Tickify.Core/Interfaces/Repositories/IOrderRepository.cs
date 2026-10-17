using Tickify.Core.DTOs.Common;
using Tickify.Core.DTOs.Orders;
using Tickify.Core.Entities;

namespace Tickify.Core.Interfaces.Repositories;

public interface IOrderRepository
{
    Task<PagedResult<Order>> GetPagedAsync(int? userId, OrderQueryParameters query, CancellationToken ct);
    /// <summary>Read-only (no tracking) order with its event and purchaser, for display.</summary>
    Task<Order?> GetByIdWithDetailsAsync(int id, CancellationToken ct);

    /// <summary>Tracked order with its seats and event, for changing it (cancel).</summary>
    Task<Order?> GetByIdWithSeatsAsync(int id, CancellationToken ct);
    Task AddAsync(Order order, CancellationToken ct);
}
