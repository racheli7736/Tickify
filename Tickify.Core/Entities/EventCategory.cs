namespace Tickify.Core.Entities;

public class EventCategory
{
    public int EventId { get; set; }
    public int CategoryId { get; set; }

    public Event Event { get; set; } = default!;
    public Category Category { get; set; } = default!;
}
