using Tickify.Core.DTOs.Categories;
using Tickify.Core.Entities;

namespace Tickify.Service.Mapping;

public static class CategoryMappings
{
    public static CategoryResponse ToResponse(this Category category) => new()
    {
        Id = category.Id,
        Name = category.Name
    };

    public static Category ToEntity(this CategoryRequest request) => new() { Name = request.Name };
}
