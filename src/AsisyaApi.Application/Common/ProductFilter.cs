namespace AsisyaApi.Application.Common;

public enum ProductSort
{
    Id,
    Name,
    NameDesc,
    Price,
    PriceDesc,
    CreatedAt,
    CreatedAtDesc
}

/// <summary>
/// Criterios de consulta ya normalizados y validados por <see cref="Services.ProductService"/>;
/// los repositorios pueden confiar en que los valores son coherentes.
/// </summary>
public sealed record ProductFilter(
    int Page,
    int PageSize,
    string? Search,
    int? CategoryId,
    decimal? MinPrice,
    decimal? MaxPrice,
    ProductSort Sort);
