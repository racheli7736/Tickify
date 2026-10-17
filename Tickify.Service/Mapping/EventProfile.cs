using AutoMapper;
using Tickify.Core.DTOs.Events;
using Tickify.Core.Entities;

namespace Tickify.Service.Mapping;

public class EventProfile : Profile
{
    public EventProfile()
    {
        // AvailableSeats / Rows / SeatsPerRow come from the seat repository, filled in by EventService.
        CreateMap<Event, EventResponse>()
            .ForMember(d => d.Categories, o => o.MapFrom(s => s.EventCategories.Select(ec => ec.Category.Name)))
            .ForMember(d => d.AvailableSeats, o => o.Ignore());

        CreateMap<Event, EventDetailsResponse>()
            .IncludeBase<Event, EventResponse>()
            .ForMember(d => d.Rows, o => o.Ignore())
            .ForMember(d => d.SeatsPerRow, o => o.Ignore());

        CreateMap<CreateEventRequest, Event>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.Seats, o => o.Ignore())
            .ForMember(d => d.EventCategories, o => o.Ignore())
            .ForMember(d => d.Orders, o => o.Ignore());

        CreateMap<UpdateEventRequest, Event>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.Seats, o => o.Ignore())
            .ForMember(d => d.EventCategories, o => o.Ignore())
            .ForMember(d => d.Orders, o => o.Ignore());
    }
}
