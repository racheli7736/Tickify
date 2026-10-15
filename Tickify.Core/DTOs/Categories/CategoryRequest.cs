using System.ComponentModel.DataAnnotations;

namespace Tickify.Core.DTOs.Categories;

public class CategoryRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
}
