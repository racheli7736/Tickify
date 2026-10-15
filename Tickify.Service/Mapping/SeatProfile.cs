using AutoMapper;
using Tickify.Core.DTOs.Seats;
using Tickify.Core.Entities;

namespace Tickify.Service.Mapping;

public class SeatProfile : Profile
{
    public SeatProfile()
    {
        CreateMap<Seat, SeatResponse>();
    }
}
