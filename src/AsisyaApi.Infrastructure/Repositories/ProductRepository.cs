using AsisyaApi.Application.Common;
using AsisyaApi.Application.Interfaces;
using AsisyaApi.Domain.Entities;
using AsisyaApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AsisyaApi.Infrastructure.Repositories;

public class ProductRepository(AppDbContext context) : IProductRepository
{
    public Task<Product?> GetByIdAsync(int id, CancellationToken ct = default) =>
        context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(ProductFilter filter, CancellationToken ct = default)
    {
        var query = context.Products.AsNoTracking().ApplyFilter(filter);

        var total = await query.CountAsync(ct);
        var items = await query
            .Include(p => p.Category)
            .ApplySort(filter.Sort)
            .ApplyPaging(filter.Page, filter.PageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(Product product, CancellationToken ct = default)
    {
        context.Products.Add(product);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        context.Products.Update(product);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Product product, CancellationToken ct = default)
    {
        context.Products.Remove(product);
        await context.SaveChangesAsync(ct);
    }
}
