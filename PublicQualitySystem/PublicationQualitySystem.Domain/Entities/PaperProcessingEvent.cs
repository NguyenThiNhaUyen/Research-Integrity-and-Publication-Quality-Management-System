using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Domain.Entities;

public class PaperProcessingEvent : BaseEntity
{
    public long PaperId { get; set; }
    public Paper Paper { get; set; } = null!;
    public long PaperVersionId { get; set; }
    public PaperVersion PaperVersion { get; set; } = null!;
    public long TrackerId { get; set; }
    public PaperProcessingTracker Tracker { get; set; } = null!;
    public string? EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public ProcessingStage Stage { get; set; }
    public ProcessingStatus Status { get; set; }
    public string? PayloadJson { get; set; }
    public string? ErrorMessage { get; set; }
}
