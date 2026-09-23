using AsisyaApi.Application.Interfaces;
using AsisyaApi.Domain.Entities;
using AsisyaApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AsisyaApi.Infrastructure.Repositories;

public class BulkJobRepository(AppDbContext context) : IBulkJobRepository
{
    public Task<BulkJob?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        context.BulkJobs.FirstOrDefaultAsync(j => j.Id == id, ct);

    public async Task AddAsync(BulkJob job, CancellationToken ct = default)
    {
        context.BulkJobs.Add(job);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(BulkJob job, CancellationToken ct = default)
    {
        context.BulkJobs.Update(job);
        await context.SaveChangesAsync(ct);
    }
}
