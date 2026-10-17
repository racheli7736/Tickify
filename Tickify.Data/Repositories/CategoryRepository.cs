using Microsoft.EntityFrameworkCore;
using Tickify.Core.Entities;
using Tickify.Core.Interfaces.Repositories;

namespace Tickify.Data.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _db;

    public CategoryRepository(AppDbContext db) => _db = db;

    public Task<List<Category>> GetAllAsync(CancellationToken ct) =>
        _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    // Tracked: used by update/delete.
    public Task<Category?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Category?> GetByIdReadOnlyAsync(int id, CancellationToken ct) =>
        _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<List<Category>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct)
    {
        var idList = ids.Distinct().ToList();
        return _db.Categories.AsNoTracking().Where(c => idList.Contains(c.Id)).ToListAsync(ct);
    }

    public Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct) =>
        // ToLower() translates to lower("Name"), matching the unique expression index.
        _db.Categories.AsNoTracking().AnyAsync(
            c => c.Name.ToLower() == name.ToLower() && (excludeId == null || c.Id != excludeId), ct);

    public Task<bool> HasEventsAsync(int id, CancellationToken ct) =>
        _db.EventCategories.AsNoTracking().AnyAsync(ec => ec.CategoryId == id, ct);

    public async Task AddAsync(Category category, CancellationToken ct) =>
        await _db.Categories.AddAsync(category, ct);

    public void Remove(Category category) => _db.Categories.Remove(category);
}
