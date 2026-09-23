using System.ComponentModel.DataAnnotations;

namespace AsisyaApi.Application.DTOs;

public sealed class BulkProductRequest
{
    public const int MaxCount = 1_000_000;

    /// <summary>Cantidad de productos aleatorios a generar.</summary>
    [Range(1, MaxCount)]
    public int Count { get; init; } = 100_000;

    /// <summary>
    /// Categorías entre las que se repartirán los productos. Si se omite, se usan todas las
    /// categorías existentes (p. ej. SERVIDORES y CLOUD creadas con scripts/seed.sh).
    /// </summary>
    public IReadOnlyList<int>? CategoryIds { get; init; }
}

public sealed record BulkJobResponse(
    Guid JobId,
    string Status,
    int TotalRecords,
    int ProcessedRecords,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    string? ErrorMessage);

/// <summary>Mensaje que viaja por la cola en memoria hacia el procesador en segundo plano.</summary>
public sealed record BulkLoadWorkItem(Guid JobId, int Count, IReadOnlyList<int> CategoryIds);
