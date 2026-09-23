using AsisyaApi.Domain.Entities;

namespace AsisyaApi.Application.Interfaces;

/// <summary>
/// Escritura masiva de productos. La implementación usa el protocolo COPY binario de PostgreSQL,
/// nunca inserciones fila a fila.
/// </summary>
public interface IBulkProductWriter
{
    /// <returns>Número de filas insertadas.</returns>
    Task<ulong> WriteAsync(IEnumerable<Product> products, CancellationToken ct = default);
}
