using AsisyaApi.Application.Common;
using AsisyaApi.Domain.Entities;

namespace AsisyaApi.Application.Interfaces;

public interface IProductRepository
{
    /// <summary>Obtiene el producto con su categoría cargada.</summary>
    Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Página de productos (con su categoría) y total de coincidencias.</summary>
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(ProductFilter filter, CancellationToken ct = default);

    Task AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    Task DeleteAsync(Product product, CancellationToken ct = default);
}
