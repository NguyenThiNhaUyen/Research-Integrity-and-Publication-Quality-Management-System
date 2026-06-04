using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Domain.Entities;

public class PaperProcessingTracker : BaseEntity
{
    public long PaperId { get; set; }
    public Paper Paper { get; set; } = null!;
    public long PaperVersionId { get; set; }
    public PaperVersion PaperVersion { get; set; } = null!;
    public string? CorrelationId { get; set; }
    public ProcessingStage CurrentStage { get; set; } = ProcessingStage.UPLOADED;
    public ProcessingStatus CurrentStatus { get; set; } = ProcessingStatus.PENDING;
    public ProcessingStatus OverallStatus { get; set; } = ProcessingStatus.PENDING;
    public int ProgressPercent { get; set; }
    public string? LastError { get; set; }
    public int RetryCount { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public ICollection<PaperProcessingEvent> Events { get; set; } = new List<PaperProcessingEvent>();
}
