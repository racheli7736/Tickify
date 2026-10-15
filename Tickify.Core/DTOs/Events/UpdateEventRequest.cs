using System.ComponentModel.DataAnnotations;

namespace Tickify.Core.DTOs.Events;

// Seat structure is intentionally absent: it is fixed at creation time.
public class UpdateEventRequest
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Venue { get; set; } = string.Empty;

    [Required]
    public DateTime StartsAt { get; set; }

    [Range(0.01, 100000)]
    public decimal TicketPrice { get; set; }

    public List<int> CategoryIds { get; set; } = new();
}
