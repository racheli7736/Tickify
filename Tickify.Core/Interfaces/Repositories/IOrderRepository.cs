using Tickify.Core.DTOs.Common;
using Tickify.Core.DTOs.Orders;
using Tickify.Core.Entities;

namespace Tickify.Core.Interfaces.Repositories;

public interface IOrderRepository
{
    Task<PagedResult<Order>> GetPagedAsync(int? userId, OrderQueryParameters query, CancellationToken ct);
    Task<Order?> GetByIdWithSeatsAsync(int id, CancellationToken ct);
    Task AddAsync(Order order, CancellationToken ct);
}
