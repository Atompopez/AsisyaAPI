using AsisyaApi.Application.Common;
using AsisyaApi.Application.DTOs;
using AsisyaApi.Application.Interfaces;

namespace AsisyaApi.Application.Services;

public class ProductService(IProductRepository products, ICategoryRepository categories)
{
    private static readonly Dictionary<string, ProductSort> SortOptions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = ProductSort.Id,
        ["name"] = ProductSort.Name,
        ["name_desc"] = ProductSort.NameDesc,
        ["price"] = ProductSort.Price,
        ["price_desc"] = ProductSort.PriceDesc,
        ["createdAt"] = ProductSort.CreatedAt,
        ["createdAt_desc"] = ProductSort.CreatedAtDesc
    };

    public async Task<PagedResult<ProductResponse>> GetPagedAsync(ProductQueryParameters query, CancellationToken ct = default)
    {
        var filter = BuildFilter(query);
        var (items, total) = await products.GetPagedAsync(filter, ct);
        return new PagedResult<ProductResponse>(
            items.Select(p => p.ToResponse()).ToList(),
            filter.Page,
            filter.PageSize,
            total);
    }

    public async Task<ProductDetailResponse> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var product = await products.GetByIdAsync(id, ct) ?? throw ProductNotFound(id);
        var category = product.Category
            ?? await categories.GetByIdAsync(product.CategoryId, ct)
            ?? throw new NotFoundException($"La categoría {product.CategoryId} no existe.");
        return product.ToDetailResponse(category);
    }

    public async Task<ProductDetailResponse> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var category = await categories.GetByIdAsync(request.CategoryId, ct) ?? throw CategoryMissing(request.CategoryId);

        var product = request.ToEntity();
        await products.AddAsync(product, ct);
        return product.ToDetailResponse(category);
    }

    public async Task<ProductDetailResponse> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await products.GetByIdAsync(id, ct) ?? throw ProductNotFound(id);
        var category = await categories.GetByIdAsync(request.CategoryId, ct) ?? throw CategoryMissing(request.CategoryId);

        request.ApplyTo(product);
        product.Category = category;
        await products.UpdateAsync(product, ct);
        return product.ToDetailResponse(category);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var product = await products.GetByIdAsync(id, ct) ?? throw ProductNotFound(id);
        await products.DeleteAsync(product, ct);
    }

    /// <summary>Normaliza y valida los parámetros de consulta antes de llegar al repositorio.</summary>
    public static ProductFilter BuildFilter(ProductQueryParameters query)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = query.PageSize switch
        {
            < 1 => ProductQueryDefaults.DefaultPageSize,
            > ProductQueryDefaults.MaxPageSize => ProductQueryDefaults.MaxPageSize,
            _ => query.PageSize
        };

        if (query.MinPrice < 0 || query.MaxPrice < 0)
        {
            throw new BusinessValidationException("Los filtros de precio no pueden ser negativos.");
        }

        if (query.MinPrice.HasValue && query.MaxPrice.HasValue && query.MinPrice > query.MaxPrice)
        {
            throw new BusinessValidationException("minPrice no puede ser mayor que maxPrice.");
        }

        var sort = ProductSort.Id;
        if (!string.IsNullOrWhiteSpace(query.SortBy) && !SortOptions.TryGetValue(query.SortBy.Trim(), out sort))
        {
            throw new BusinessValidationException(
                $"sortBy inválido. Valores permitidos: {string.Join(", ", SortOptions.Keys)}.");
        }

        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        return new ProductFilter(page, pageSize, search, query.CategoryId, query.MinPrice, query.MaxPrice, sort);
    }

    private static NotFoundException ProductNotFound(int id) => new($"El producto {id} no existe.");

    private static BusinessValidationException CategoryMissing(int categoryId) =>
        new($"La categoría {categoryId} no existe.");
}
