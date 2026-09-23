using AsisyaApi.Application.DTOs;
using AsisyaApi.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsisyaApi.Api.Controllers;

[ApiController]
[Authorize]
[Route("Category")]
[Produces("application/json")]
public class CategoryController(CategoryService categoryService) : ControllerBase
{
    /// <summary>Crea una categoría (p. ej. SERVIDORES o CLOUD).</summary>
    [HttpPost]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> Create(CreateCategoryRequest request, CancellationToken ct)
    {
        var category = await categoryService.CreateAsync(request, ct);
        return Created($"/Category/{category.Id}", category);
    }

    /// <summary>Lista las categorías (usado por el formulario de productos del frontend).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategoryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetAll(CancellationToken ct) =>
        Ok(await categoryService.GetAllAsync(ct));
}
