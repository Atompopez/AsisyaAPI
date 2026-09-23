using AsisyaApi.Application.Interfaces;
using AsisyaApi.Domain.Entities;
using AsisyaApi.Infrastructure.Persistence.Configurations;
using Npgsql;
using NpgsqlTypes;

namespace AsisyaApi.Infrastructure.BulkLoad;

/// <summary>
/// Inserta productos con el protocolo COPY ... FROM STDIN (FORMAT BINARY) de PostgreSQL mediante
/// <see cref="NpgsqlBinaryImporter"/>: un único round-trip en streaming por lote, sin el costo de
/// change tracking ni de un INSERT por fila.
/// </summary>
public class BulkProductWriter(NpgsqlDataSource dataSource) : IBulkProductWriter
{
    private const string CopyCommand =
        $"COPY \"{ProductConfiguration.TableName}\" (\"Name\", \"Description\", \"Price\", \"Stock\", \"CategoryId\", \"CreatedAt\") " +
        "FROM STDIN (FORMAT BINARY)";

    public async Task<ulong> WriteAsync(IEnumerable<Product> products, CancellationToken ct = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var importer = await connection.BeginBinaryImportAsync(CopyCommand, ct);

        foreach (var product in products)
        {
            await importer.StartRowAsync(ct);
            await importer.WriteAsync(product.Name, NpgsqlDbType.Varchar, ct);
            await importer.WriteAsync(product.Description, NpgsqlDbType.Varchar, ct);
            await importer.WriteAsync(product.Price, NpgsqlDbType.Numeric, ct);
            await importer.WriteAsync(product.Stock, NpgsqlDbType.Integer, ct);
            await importer.WriteAsync(product.CategoryId, NpgsqlDbType.Integer, ct);
            await importer.WriteAsync(DateTime.SpecifyKind(product.CreatedAt, DateTimeKind.Utc), NpgsqlDbType.TimestampTz, ct);
        }

        // Sin CompleteAsync el COPY se descarta al hacer Dispose (rollback implícito del lote).
        return await importer.CompleteAsync(ct);
    }
}
