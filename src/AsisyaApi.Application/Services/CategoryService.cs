using AsisyaApi.Application.Common;
using AsisyaApi.Application.DTOs;
using AsisyaApi.Application.Interfaces;

namespace AsisyaApi.Application.Services;

public class CategoryService(ICategoryRepository categories)
{
    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new BusinessValidationException("El nombre de la categoría es obligatorio.");
        }

        if (await categories.ExistsByNameAsync(request.Name.Trim(), ct))
        {
            throw new ConflictException($"Ya existe una categoría llamada '{request.Name.Trim()}'.");
        }

        var category = request.ToEntity();
        await categories.AddAsync(category, ct);
        return category.ToResponse();
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var all = await categories.GetAllAsync(ct);
        return all.Select(c => c.ToResponse()).ToList();
    }
}
