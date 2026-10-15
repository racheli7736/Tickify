namespace Tickify.Core.DTOs.Events;

public class EventResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public decimal TicketPrice { get; set; }
    public List<string> Categories { get; set; } = new();
    public int AvailableSeats { get; set; }
}
