using AsisyaApi.Application.DTOs;

namespace AsisyaApi.Application.Interfaces;

/// <summary>Cola de trabajos de carga masiva (en memoria hoy; reemplazable por un broker).</summary>
public interface IBulkLoadQueue
{
    ValueTask EnqueueAsync(BulkLoadWorkItem item, CancellationToken ct = default);
}
