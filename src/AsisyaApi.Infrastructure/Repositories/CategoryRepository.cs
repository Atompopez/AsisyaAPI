using AsisyaApi.Application.Common;
using AsisyaApi.Application.Interfaces;
using AsisyaApi.Domain.Entities;
using AsisyaApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AsisyaApi.Infrastructure.Repositories;

public class CategoryRepository(AppDbContext context) : ICategoryRepository
{
    public Task<Category?> GetByIdAsync(int id, CancellationToken ct = default) =>
        context.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default) =>
        context.Categories.AnyAsync(c => c.Id == id, ct);

    public Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
    {
        var normalized = name.ToUpper();
        return context.Categories.AnyAsync(c => c.Name.ToUpper() == normalized, ct);
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default) =>
        await context.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<int>> GetExistingIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default) =>
        await context.Categories.Where(c => ids.Contains(c.Id)).Select(c => c.Id).ToListAsync(ct);

    public async Task AddAsync(Category category, CancellationToken ct = default)
    {
        context.Categories.Add(category);
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            throw new ConflictException($"Ya existe una categoría llamada '{category.Name}'.");
        }
    }
}
