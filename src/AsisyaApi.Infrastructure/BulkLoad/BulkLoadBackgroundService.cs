using AsisyaApi.Application.Services;
using AsisyaApi.Domain.Enums;
using AsisyaApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AsisyaApi.Infrastructure.BulkLoad;

/// <summary>
/// Consumidor único de <see cref="BulkLoadChannel"/>. Cada job se procesa en su propio scope de DI
/// (DbContext nuevo) delegando la lógica en <see cref="BulkLoadService.ProcessAsync"/>.
/// </summary>
public class BulkLoadBackgroundService(
    BulkLoadChannel channel,
    IServiceScopeFactory scopeFactory,
    ILogger<BulkLoadBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await FailOrphanedJobsAsync(stoppingToken);

        await foreach (var item in channel.Reader.ReadAllAsync(stoppingToken))
        {
            logger.LogInformation("Iniciando carga masiva {JobId}: {Count} productos", item.JobId, item.Count);
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<BulkLoadService>();
                await service.ProcessAsync(item, stoppingToken);
                logger.LogInformation("Carga masiva {JobId} finalizada", item.JobId);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error no controlado procesando la carga masiva {JobId}", item.JobId);
            }
        }
    }

    /// <summary>
    /// La cola vive en memoria: si la API se reinicia, los jobs pendientes o en curso se pierden.
    /// Se marcan como fallidos para que el cliente que hace polling no espere indefinidamente.
    /// </summary>
    private async Task FailOrphanedJobsAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTime.UtcNow;
            var affected = await context.BulkJobs
                .Where(j => j.Status == BulkJobStatus.Pending || j.Status == BulkJobStatus.Processing)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(j => j.Status, BulkJobStatus.Failed)
                    .SetProperty(j => j.CompletedAt, now)
                    .SetProperty(j => j.ErrorMessage, "Interrumpido por un reinicio de la API."), ct);

            if (affected > 0)
            {
                logger.LogWarning("{Count} jobs de carga masiva interrumpidos por reinicio marcados como Failed", affected);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "No se pudieron revisar los jobs huérfanos al iniciar");
        }
    }
}
