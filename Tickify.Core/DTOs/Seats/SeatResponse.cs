using Tickify.Core.Enums;

namespace Tickify.Core.DTOs.Seats;

public class SeatResponse
{
    public int Id { get; set; }
    public int Row { get; set; }
    public int Number { get; set; }
    public SeatStatus Status { get; set; }
}
