using AsisyaApi.Domain.Entities;

namespace AsisyaApi.Application.Interfaces;

public interface IBulkJobRepository
{
    Task<BulkJob?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(BulkJob job, CancellationToken ct = default);
    Task UpdateAsync(BulkJob job, CancellationToken ct = default);
}
