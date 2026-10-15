namespace Tickify.Core.Entities;

public class Event
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public decimal TicketPrice { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    public ICollection<EventCategory> EventCategories { get; set; } = new List<EventCategory>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
