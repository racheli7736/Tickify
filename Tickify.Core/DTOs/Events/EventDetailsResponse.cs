namespace Tickify.Core.DTOs.Events;

public class EventDetailsResponse : EventResponse
{
    public string Description { get; set; } = string.Empty;
    public int Rows { get; set; }
    public int SeatsPerRow { get; set; }
    public DateTime CreatedAt { get; set; }
}
