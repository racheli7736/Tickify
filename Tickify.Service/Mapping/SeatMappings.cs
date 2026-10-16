using Tickify.Core.DTOs.Seats;
using Tickify.Core.Entities;

namespace Tickify.Service.Mapping;

public static class SeatMappings
{
    public static SeatResponse ToResponse(this Seat seat) => new()
    {
        Id = seat.Id,
        Row = seat.Row,
        Number = seat.Number,
        Status = seat.Status
    };
}
