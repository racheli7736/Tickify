using Tickify.Core.Enums;

namespace Tickify.Core.Entities;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int EventId { get; set; }
    public DateTime CreatedAt { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Active;
    public decimal TotalPrice { get; set; }
    public string SeatsSummary { get; set; } = string.Empty;

    public User User { get; set; } = default!;
    public Event Event { get; set; } = default!;
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
