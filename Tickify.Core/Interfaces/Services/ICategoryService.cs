using Tickify.Core.Common;
using Tickify.Core.DTOs.Categories;

namespace Tickify.Core.Interfaces.Services;

public interface ICategoryService
{
    Task<ServiceResult<List<CategoryResponse>>> GetAllAsync(CancellationToken ct);
    Task<ServiceResult<CategoryResponse>> CreateAsync(CategoryRequest request, CancellationToken ct);
    Task<ServiceResult> UpdateAsync(int id, CategoryRequest request, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken ct);
}
