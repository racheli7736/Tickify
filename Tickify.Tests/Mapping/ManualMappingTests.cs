using Tickify.Core.DTOs.Events;
using Tickify.Core.Entities;
using Tickify.Core.Enums;
using Tickify.Service.Mapping;

namespace Tickify.Tests.Mapping;

public class ManualMappingTests
{
    [Fact]
    public void Event_ToResponse_MapsFieldsAndCategoryNames()
    {
        var ev = new Event
        {
            Id = 7, Name = "Show", Venue = "Hall", TicketPrice = 50m,
            StartsAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EventCategories = { new EventCategory { Category = new Category { Name = "Music" } } }
        };

        var response = ev.ToResponse();

        Assert.Equal(7, response.Id);
        Assert.Equal("Show", response.Name);
        Assert.Equal(50m, response.TicketPrice);
        Assert.Equal(new[] { "Music" }, response.Categories);
    }

    [Fact]
    public void UpdateEventRequest_ApplyTo_ChangesDetailsOnly()
    {
        var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var ev = new Event { Id = 3, Name = "Old", CreatedAt = created };
        var request = new UpdateEventRequest
        {
            Name = "New", Venue = "V", Description = "D", TicketPrice = 10m, StartsAt = DateTime.UtcNow
        };

        request.ApplyTo(ev);

        Assert.Equal("New", ev.Name);
        Assert.Equal(3, ev.Id);
        Assert.Equal(created, ev.CreatedAt);
    }

    [Fact]
    public void Order_ToResponse_WorksWithAndWithoutLoadedEvent()
    {
        var order = new Order
        {
            Id = 1, EventId = 2, TotalPrice = 20m, SeatsSummary = "R1-S1", Status = OrderStatus.Active,
            Event = null!
        };

        Assert.Equal(string.Empty, order.ToResponse().EventName);

        order.Event = new Event { Name = "Show", StartsAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        var response = order.ToResponse();

        Assert.Equal("Show", response.EventName);
        Assert.Equal(order.Event.StartsAt, response.EventStartsAt);
    }
}
