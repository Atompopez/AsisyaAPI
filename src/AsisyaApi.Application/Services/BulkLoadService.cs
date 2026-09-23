using AsisyaApi.Application.Common;
using AsisyaApi.Application.DTOs;
using AsisyaApi.Application.Interfaces;
using AsisyaApi.Domain.Entities;
using AsisyaApi.Domain.Enums;

namespace AsisyaApi.Application.Services;

/// <summary>
/// Casos de uso de la carga masiva: encolar un job (respuesta inmediata) y procesarlo en segundo
/// plano en lotes, actualizando el progreso tras cada lote.
/// </summary>
public class BulkLoadService(
    IBulkJobRepository jobs,
    ICategoryRepository categories,
    IBulkLoadQueue queue,
    IBulkProductWriter writer)
{
    /// <summary>Filas por lote de COPY; también es la frecuencia con la que se reporta progreso.</summary>
    public const int BatchSize = 5_000;

    private static readonly string[] Adjectives =
        ["Pro", "Max", "Lite", "Plus", "Ultra", "Edge", "Core", "Flex", "Prime", "Nano"];

    private static readonly string[] Nouns =
        ["Servidor Rack", "Blade", "Instancia VM", "Almacenamiento", "Balanceador", "Firewall", "Switch", "Backup", "Kubernetes Node", "CDN"];

    public async Task<BulkJobResponse> StartAsync(BulkProductRequest request, CancellationToken ct = default)
    {
        if (request.Count is < 1 or > BulkProductRequest.MaxCount)
        {
            throw new BusinessValidationException($"count debe estar entre 1 y {BulkProductRequest.MaxCount}.");
        }

        var categoryIds = await ResolveCategoryIdsAsync(request.CategoryIds, ct);

        var job = new BulkJob
        {
            Id = Guid.NewGuid(),
            Status = BulkJobStatus.Pending,
            TotalRecords = request.Count,
            ProcessedRecords = 0,
            CreatedAt = DateTime.UtcNow
        };
        await jobs.AddAsync(job, ct);
        await queue.EnqueueAsync(new BulkLoadWorkItem(job.Id, request.Count, categoryIds), ct);

        return job.ToResponse();
    }

    public async Task<BulkJobResponse> GetStatusAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await jobs.GetByIdAsync(jobId, ct) ?? throw new NotFoundException($"El job {jobId} no existe.");
        return job.ToResponse();
    }

    /// <summary>Ejecuta un job: lo invoca el consumidor en segundo plano de la cola.</summary>
    public async Task ProcessAsync(BulkLoadWorkItem item, CancellationToken ct = default)
    {
        var job = await jobs.GetByIdAsync(item.JobId, ct);
        if (job is null || job.Status != BulkJobStatus.Pending)
        {
            return;
        }

        job.Status = BulkJobStatus.Processing;
        await jobs.UpdateAsync(job, ct);

        try
        {
            var processed = 0;
            while (processed < item.Count)
            {
                ct.ThrowIfCancellationRequested();
                var size = Math.Min(BatchSize, item.Count - processed);
                var batch = GenerateProducts(processed, size, item.CategoryIds);

                var written = await writer.WriteAsync(batch, ct);
                processed += (int)written;

                job.ProcessedRecords = processed;
                await jobs.UpdateAsync(job, ct);
            }

            job.Status = BulkJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            await jobs.UpdateAsync(job, CancellationToken.None);
        }
        catch (Exception ex)
        {
            job.Status = BulkJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            job.ErrorMessage = ex is OperationCanceledException
                ? "El procesamiento se canceló (apagado de la aplicación)."
                : ex.Message;
            await jobs.UpdateAsync(job, CancellationToken.None);
        }
    }

    /// <summary>Genera productos pseudoaleatorios repartidos en round-robin entre las categorías.</summary>
    public static IEnumerable<Product> GenerateProducts(int startIndex, int count, IReadOnlyList<int> categoryIds)
    {
        var now = DateTime.UtcNow;
        for (var i = 0; i < count; i++)
        {
            var index = startIndex + i;
            var random = Random.Shared;
            yield return new Product
            {
                Name = $"{Nouns[random.Next(Nouns.Length)]} {Adjectives[random.Next(Adjectives.Length)]} #{index + 1}",
                Description = $"Producto generado por carga masiva ({index + 1}).",
                Price = Math.Round((decimal)(random.NextDouble() * 9_990 + 10), 2),
                Stock = random.Next(0, 501),
                CategoryId = categoryIds[index % categoryIds.Count],
                CreatedAt = now
            };
        }
    }

    private async Task<IReadOnlyList<int>> ResolveCategoryIdsAsync(IReadOnlyList<int>? requested, CancellationToken ct)
    {
        if (requested is { Count: > 0 })
        {
            var distinct = requested.Distinct().ToList();
            var existing = await categories.GetExistingIdsAsync(distinct, ct);
            var missing = distinct.Except(existing).ToList();
            if (missing.Count > 0)
            {
                throw new BusinessValidationException($"Categorías inexistentes: {string.Join(", ", missing)}.");
            }

            return distinct;
        }

        var all = await categories.GetAllAsync(ct);
        if (all.Count == 0)
        {
            throw new BusinessValidationException(
                "No hay categorías. Créelas primero con POST /Category (ver scripts/seed.sh).");
        }

        return all.Select(c => c.Id).ToList();
    }
}
