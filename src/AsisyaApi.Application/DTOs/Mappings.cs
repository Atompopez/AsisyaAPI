using AsisyaApi.Domain.Entities;

namespace AsisyaApi.Application.DTOs;

/// <summary>Mapeo explícito entre entidades y DTOs. Las entidades nunca salen de Application.</summary>
public static class Mappings
{
    public static CategoryResponse ToResponse(this Category category) =>
        new(category.Id, category.Name, category.PhotoUrl);

    public static ProductResponse ToResponse(this Product product) =>
        new(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.Stock,
            product.CategoryId,
            product.Category?.Name ?? string.Empty,
            product.CreatedAt);

    public static ProductDetailResponse ToDetailResponse(this Product product, Category category) =>
        new(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.Stock,
            product.CreatedAt,
            category.ToResponse());

    public static BulkJobResponse ToResponse(this BulkJob job) =>
        new(
            job.Id,
            job.Status.ToString(),
            job.TotalRecords,
            job.ProcessedRecords,
            job.CreatedAt,
            job.CompletedAt,
            job.ErrorMessage);

    public static Category ToEntity(this CreateCategoryRequest request) =>
        new() { Name = request.Name.Trim(), PhotoUrl = request.PhotoUrl.Trim() };

    public static Product ToEntity(this CreateProductRequest request) =>
        new()
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Price = request.Price,
            Stock = request.Stock,
            CategoryId = request.CategoryId,
            CreatedAt = DateTime.UtcNow
        };

    public static void ApplyTo(this UpdateProductRequest request, Product product)
    {
        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim() ?? string.Empty;
        product.Price = request.Price;
        product.Stock = request.Stock;
        product.CategoryId = request.CategoryId;
    }
}
