using System.ComponentModel.DataAnnotations;
using Tickify.Core.Enums;

namespace Tickify.Core.Entities;

public class Seat
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int Row { get; set; }
    public int Number { get; set; }
    public SeatStatus Status { get; set; } = SeatStatus.Available;
    public int? OrderId { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;

    public Event Event { get; set; } = default!;
    public Order? Order { get; set; }
}
