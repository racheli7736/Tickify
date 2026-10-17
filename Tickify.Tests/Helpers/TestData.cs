using Tickify.Core.Entities;
using Tickify.Core.Enums;

namespace Tickify.Tests.Helpers;

/// <summary>Small builders so each test only states what matters to it.</summary>
public static class TestData
{
    public static DateTime Future => DateTime.UtcNow.AddDays(30);
    public static DateTime Past => DateTime.UtcNow.AddDays(-1);

    public static Event Event(int id = 1, DateTime? startsAt = null, decimal price = 50m) => new()
    {
        Id = id,
        Name = "Show",
        Venue = "Hall",
        StartsAt = startsAt ?? Future,
        TicketPrice = price,
        CreatedAt = DateTime.UtcNow
    };

    public static Seat Seat(int id, int eventId = 1, int row = 1, int number = 1, SeatStatus status = SeatStatus.Available) => new()
    {
        Id = id,
        EventId = eventId,
        Row = row,
        Number = number,
        Status = status
    };

    /// <summary>Seats 1..count in row 1, all available.</summary>
    public static List<Seat> AvailableSeats(int count, int eventId = 1) =>
        Enumerable.Range(1, count).Select(i => Seat(i, eventId, number: i)).ToList();

    /// <summary>An active order that owns the given seats (which are marked sold).</summary>
    public static Order ActiveOrder(int id, int userId, Event ev, params Seat[] seats)
    {
        var order = new Order
        {
            Id = id,
            UserId = userId,
            EventId = ev.Id,
            Event = ev,
            User = new User { Id = userId, FullName = $"Buyer {userId}", Email = $"buyer{userId}@example.com" },
            Status = OrderStatus.Active,
            TotalPrice = ev.TicketPrice * seats.Length,
            SeatsSummary = "summary",
            CreatedAt = DateTime.UtcNow
        };
        foreach (var seat in seats)
        {
            seat.Status = SeatStatus.Sold;
            seat.OrderId = id;
            seat.Order = order;
            order.Seats.Add(seat);
        }
        return order;
    }
}
