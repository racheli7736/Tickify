using Tickify.Core.Enums;

namespace Tickify.Core.DTOs.Orders;

public class OrderResponse
{
    public int Id { get; set; }
    /// <summary>Who bought the order. Only the id and the full name are exposed (no e-mail), so admins can tell orders apart.</summary>
    public int UserId { get; set; }
    public string? UserFullName { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventStartsAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalPrice { get; set; }
    public string SeatsSummary { get; set; } = string.Empty;
}
