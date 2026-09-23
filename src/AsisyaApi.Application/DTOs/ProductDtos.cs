using System.ComponentModel.DataAnnotations;

namespace AsisyaApi.Application.DTOs;

public sealed class CreateProductRequest
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; init; } = string.Empty;

    [Range(typeof(decimal), "0", "99999999")]
    public decimal Price { get; init; }

    [Range(0, int.MaxValue)]
    public int Stock { get; init; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; init; }
}

public sealed class UpdateProductRequest
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; init; } = string.Empty;

    [Range(typeof(decimal), "0", "99999999")]
    public decimal Price { get; init; }

    [Range(0, int.MaxValue)]
    public int Stock { get; init; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; init; }
}

/// <summary>Parámetros de consulta de GET /Products (todos opcionales).</summary>
public sealed class ProductQueryParameters
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = ProductQueryDefaults.DefaultPageSize;
    public string? Search { get; init; }
    public int? CategoryId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }

    /// <summary>id | name | name_desc | price | price_desc | createdAt | createdAt_desc</summary>
    public string? SortBy { get; init; }
}

public static class ProductQueryDefaults
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}

/// <summary>Elemento del listado paginado.</summary>
public sealed record ProductResponse(
    int Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    int CategoryId,
    string CategoryName,
    DateTime CreatedAt);

/// <summary>Detalle de un producto, incluye nombre y foto de su categoría.</summary>
public sealed record ProductDetailResponse(
    int Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    DateTime CreatedAt,
    CategoryResponse Category);
