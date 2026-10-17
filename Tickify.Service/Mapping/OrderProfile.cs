using AutoMapper;
using Tickify.Core.DTOs.Orders;
using Tickify.Core.Entities;

namespace Tickify.Service.Mapping;

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderResponse>()
            .ForMember(d => d.EventName, o => o.MapFrom(s => s.Event.Name))
            .ForMember(d => d.EventStartsAt, o => o.MapFrom(s => s.Event.StartsAt));
    }
}
