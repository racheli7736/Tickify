using System.ComponentModel.DataAnnotations;

namespace Tickify.Core.DTOs.Orders;

public class CreateOrderRequest
{
    [Range(1, int.MaxValue)]
    public int EventId { get; set; }

    [Required, MinLength(1), MaxLength(6)]
    public List<int> SeatIds { get; set; } = new();
}
