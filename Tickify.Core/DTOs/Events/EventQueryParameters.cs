using System.ComponentModel.DataAnnotations;

namespace Tickify.Core.DTOs.Events;

public class EventQueryParameters
{
    public int? CategoryId { get; set; }
    public string? Search { get; set; }

    // date | price | name
    public string SortBy { get; set; } = "date";

    // asc | desc
    public string SortDir { get; set; } = "asc";

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 10;
}
