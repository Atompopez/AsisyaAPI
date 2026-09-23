using AsisyaApi.Domain.Entities;

namespace AsisyaApi.Application.Common;

/// <summary>
/// Filtros, orden y paginación expresados en LINQ puro: el repositorio EF los traduce a SQL y
/// las pruebas unitarias los ejecutan sobre colecciones en memoria.
/// </summary>
public static class ProductQueryExtensions
{
    public static IQueryable<Product> ApplyFilter(this IQueryable<Product> query, ProductFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || p.Description.ToLower().Contains(term));
        }

        if (filter.CategoryId.HasValue)
        {
            var categoryId = filter.CategoryId.Value;
            query = query.Where(p => p.CategoryId == categoryId);
        }

        if (filter.MinPrice.HasValue)
        {
            var minPrice = filter.MinPrice.Value;
            query = query.Where(p => p.Price >= minPrice);
        }

        if (filter.MaxPrice.HasValue)
        {
            var maxPrice = filter.MaxPrice.Value;
            query = query.Where(p => p.Price <= maxPrice);
        }

        return query;
    }

    public static IQueryable<Product> ApplySort(this IQueryable<Product> query, ProductSort sort) => sort switch
    {
        ProductSort.Name => query.OrderBy(p => p.Name).ThenBy(p => p.Id),
        ProductSort.NameDesc => query.OrderByDescending(p => p.Name).ThenBy(p => p.Id),
        ProductSort.Price => query.OrderBy(p => p.Price).ThenBy(p => p.Id),
        ProductSort.PriceDesc => query.OrderByDescending(p => p.Price).ThenBy(p => p.Id),
        ProductSort.CreatedAt => query.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id),
        ProductSort.CreatedAtDesc => query.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id),
        _ => query.OrderBy(p => p.Id)
    };

    public static IQueryable<Product> ApplyPaging(this IQueryable<Product> query, int page, int pageSize) =>
        query.Skip((page - 1) * pageSize).Take(pageSize);
}
