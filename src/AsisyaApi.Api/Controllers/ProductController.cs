using AsisyaApi.Application.Common;
using AsisyaApi.Application.DTOs;
using AsisyaApi.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsisyaApi.Api.Controllers;

/// <summary>
/// Rutas literales del enunciado: singular para escritura (/Product) y plural para lectura y
/// edición (/Products), por compatibilidad con colecciones Postman que usen esas rutas exactas.
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public class ProductController(ProductService productService, BulkLoadService bulkLoadService) : ControllerBase
{
    /// <summary>Crea un producto individual.</summary>
    [HttpPost("/Product")]
    [ProducesResponseType<ProductDetailResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDetailResponse>> Create(CreateProductRequest request, CancellationToken ct)
    {
        var product = await productService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// <summary>
    /// Encola la generación de N productos aleatorios (100.000 por defecto) y responde de inmediato
    /// con el jobId. La inserción real ocurre en segundo plano con COPY binario de PostgreSQL.
    /// </summary>
    [HttpPost("/Product/bulk")]
    [ProducesResponseType<BulkJobResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkJobResponse>> CreateBulk(BulkProductRequest request, CancellationToken ct)
    {
        var job = await bulkLoadService.StartAsync(request, ct);
        return AcceptedAtAction(nameof(GetBulkStatus), new { jobId = job.JobId }, job);
    }

    /// <summary>Estado de un job de carga masiva.</summary>
    [HttpGet("/Product/bulk/{jobId:guid}")]
    [ProducesResponseType<BulkJobResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BulkJobResponse>> GetBulkStatus(Guid jobId, CancellationToken ct) =>
        Ok(await bulkLoadService.GetStatusAsync(jobId, ct));

    /// <summary>Listado paginado con búsqueda, filtros y ordenamiento.</summary>
    [HttpGet("/Products")]
    [ProducesResponseType<PagedResult<ProductResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ProductResponse>>> GetAll([FromQuery] ProductQueryParameters query, CancellationToken ct) =>
        Ok(await productService.GetPagedAsync(query, ct));

    /// <summary>Detalle del producto con nombre y foto de su categoría.</summary>
    [HttpGet("/Products/{id:int}")]
    [ProducesResponseType<ProductDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailResponse>> GetById(int id, CancellationToken ct) =>
        Ok(await productService.GetByIdAsync(id, ct));

    [HttpPut("/Products/{id:int}")]
    [ProducesResponseType<ProductDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailResponse>> Update(int id, UpdateProductRequest request, CancellationToken ct) =>
        Ok(await productService.UpdateAsync(id, request, ct));

    [HttpDelete("/Products/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await productService.DeleteAsync(id, ct);
        return NoContent();
    }
}
