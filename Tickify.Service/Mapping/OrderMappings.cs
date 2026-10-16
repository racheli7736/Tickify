using Tickify.Core.DTOs.Orders;
using Tickify.Core.Entities;

namespace Tickify.Service.Mapping;

public static class OrderMappings
{
    // The event may not be loaded (a freshly created order); the caller then sets the event fields.
    public static OrderResponse ToResponse(this Order order) => new()
    {
        Id = order.Id,
        EventId = order.EventId,
        EventName = order.Event?.Name ?? string.Empty,
        EventStartsAt = order.Event?.StartsAt ?? default,
        CreatedAt = order.CreatedAt,
        Status = order.Status,
        TotalPrice = order.TotalPrice,
        SeatsSummary = order.SeatsSummary
    };
}
