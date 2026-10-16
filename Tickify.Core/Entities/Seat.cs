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

    // Optimistic concurrency token. On PostgreSQL this maps to the xmin system column,
    // which the database changes automatically on every update of the row.
    [Timestamp]
    public uint RowVersion { get; set; }

    public Event Event { get; set; } = default!;
    public Order? Order { get; set; }
}
