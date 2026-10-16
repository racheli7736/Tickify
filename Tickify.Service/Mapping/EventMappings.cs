using Tickify.Core.DTOs.Events;
using Tickify.Core.Entities;

namespace Tickify.Service.Mapping;

public static class EventMappings
{
    // AvailableSeats is filled in by EventService from the seat repository.
    public static EventResponse ToResponse(this Event ev) => new()
    {
        Id = ev.Id,
        Name = ev.Name,
        Venue = ev.Venue,
        StartsAt = ev.StartsAt,
        TicketPrice = ev.TicketPrice,
        Categories = ev.EventCategories.Select(ec => ec.Category.Name).ToList()
    };

    // AvailableSeats, Rows and SeatsPerRow are filled in by EventService.
    public static EventDetailsResponse ToDetailsResponse(this Event ev) => new()
    {
        Id = ev.Id,
        Name = ev.Name,
        Venue = ev.Venue,
        StartsAt = ev.StartsAt,
        TicketPrice = ev.TicketPrice,
        Categories = ev.EventCategories.Select(ec => ec.Category.Name).ToList(),
        Description = ev.Description,
        CreatedAt = ev.CreatedAt
    };

    // Id, CreatedAt, seats and categories are set by EventService.
    public static Event ToEntity(this CreateEventRequest request) => new()
    {
        Name = request.Name,
        Description = request.Description,
        Venue = request.Venue,
        StartsAt = request.StartsAt,
        TicketPrice = request.TicketPrice
    };

    // Only details are editable; the seat structure is fixed at creation.
    public static void ApplyTo(this UpdateEventRequest request, Event ev)
    {
        ev.Name = request.Name;
        ev.Description = request.Description;
        ev.Venue = request.Venue;
        ev.StartsAt = request.StartsAt;
        ev.TicketPrice = request.TicketPrice;
    }
}
