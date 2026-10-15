using System.ComponentModel.DataAnnotations;

namespace Tickify.Core.DTOs.Events;

public class CreateEventRequest
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

    [Range(1, 50)]
    public int Rows { get; set; }

    [Range(1, 100)]
    public int SeatsPerRow { get; set; }

    public List<int> CategoryIds { get; set; } = new();
}
