using Tickify.Core.Enums;

namespace Tickify.Core.DTOs.Orders;

public class OrderResponse
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventStartsAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalPrice { get; set; }
    public string SeatsSummary { get; set; } = string.Empty;
}
