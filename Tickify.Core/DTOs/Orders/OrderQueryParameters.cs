using System.ComponentModel.DataAnnotations;

namespace Tickify.Core.DTOs.Orders;

public class OrderQueryParameters
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 10;
}
