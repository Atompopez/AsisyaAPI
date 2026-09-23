using AsisyaApi.Domain.Enums;

namespace AsisyaApi.Domain.Entities;

public class BulkJob
{
    public Guid Id { get; set; }
    public BulkJobStatus Status { get; set; } = BulkJobStatus.Pending;
    public int TotalRecords { get; set; }
    public int ProcessedRecords { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
