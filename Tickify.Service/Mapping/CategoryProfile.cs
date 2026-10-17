using AutoMapper;
using Tickify.Core.DTOs.Categories;
using Tickify.Core.Entities;

namespace Tickify.Service.Mapping;

public class CategoryProfile : Profile
{
    public CategoryProfile()
    {
        CreateMap<Category, CategoryResponse>();

        CreateMap<CategoryRequest, Category>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.EventCategories, o => o.Ignore());
    }
}
