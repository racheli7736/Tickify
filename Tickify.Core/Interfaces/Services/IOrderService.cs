using Tickify.Core.Common;
using Tickify.Core.DTOs.Common;
using Tickify.Core.DTOs.Orders;

namespace Tickify.Core.Interfaces.Services;

public interface IOrderService
{
    Task<ServiceResult<OrderResponse>> PurchaseAsync(int userId, CreateOrderRequest request, CancellationToken ct);
    Task<ServiceResult<PagedResult<OrderResponse>>> GetPagedAsync(int userId, bool isAdmin, OrderQueryParameters query, CancellationToken ct);
    Task<ServiceResult<OrderResponse>> GetByIdAsync(int userId, bool isAdmin, int orderId, CancellationToken ct);
    Task<ServiceResult> CancelAsync(int userId, int orderId, CancellationToken ct);
}
