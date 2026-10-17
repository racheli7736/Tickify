using AutoMapper;
using Microsoft.Extensions.Logging;
using Tickify.Core.Common;
using Tickify.Core.DTOs.Categories;
using Tickify.Core.Entities;
using Tickify.Core.Exceptions;
using Tickify.Core.Interfaces.Repositories;
using Tickify.Core.Interfaces.Services;

namespace Tickify.Service.Services;

public class CategoryService : ICategoryService
{
    private const string NameTakenMessage = "A category with this name already exists.";

    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        ICategoryRepository categories,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CategoryService> logger)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ServiceResult<List<CategoryResponse>>> GetAllAsync(CancellationToken ct)
    {
        var categories = await _categories.GetAllAsync(ct);
        return ServiceResult<List<CategoryResponse>>.Success(_mapper.Map<List<CategoryResponse>>(categories));
    }

    public async Task<ServiceResult<CategoryResponse>> GetByIdAsync(int id, CancellationToken ct)
    {
        var category = await _categories.GetByIdReadOnlyAsync(id, ct);
        return category is null
            ? ServiceResult<CategoryResponse>.Fail(ErrorType.NotFound, "Category not found.")
            : ServiceResult<CategoryResponse>.Success(_mapper.Map<CategoryResponse>(category));
    }

    public async Task<ServiceResult<CategoryResponse>> CreateAsync(CategoryRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        if (await _categories.NameExistsAsync(name, null, ct))
            return ServiceResult<CategoryResponse>.Fail(ErrorType.Conflict, NameTakenMessage);

        var category = _mapper.Map<Category>(request);
        category.Name = name;
        await _categories.AddAsync(category, ct);
        if (!await TrySaveAsync(ct))
            return ServiceResult<CategoryResponse>.Fail(ErrorType.Conflict, NameTakenMessage);

        _logger.LogInformation("Category {CategoryId} created", category.Id);
        return ServiceResult<CategoryResponse>.Success(_mapper.Map<CategoryResponse>(category));
    }

    public async Task<ServiceResult> UpdateAsync(int id, CategoryRequest request, CancellationToken ct)
    {
        var category = await _categories.GetByIdAsync(id, ct);
        if (category is null)
            return ServiceResult.Fail(ErrorType.NotFound, "Category not found.");

        var name = request.Name.Trim();
        if (await _categories.NameExistsAsync(name, id, ct))
            return ServiceResult.Fail(ErrorType.Conflict, NameTakenMessage);

        category.Name = name;
        if (!await TrySaveAsync(ct))
            return ServiceResult.Fail(ErrorType.Conflict, NameTakenMessage);

        _logger.LogInformation("Category {CategoryId} updated", id);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken ct)
    {
        var category = await _categories.GetByIdAsync(id, ct);
        if (category is null)
            return ServiceResult.Fail(ErrorType.NotFound, "Category not found.");

        if (await _categories.HasEventsAsync(id, ct))
            return ServiceResult.Fail(ErrorType.Conflict, "Cannot delete a category that is assigned to events.");

        _categories.Remove(category);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Category {CategoryId} deleted", id);
        return ServiceResult.Success();
    }

    // False when another request took the same name between our check and our save (unique index on lower(Name)).
    private async Task<bool> TrySaveAsync(CancellationToken ct)
    {
        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
            return true;
        }
        catch (DuplicateKeyException)
        {
            _logger.LogWarning("Category save lost a race for a name that was just taken");
            return false;
        }
    }
}
