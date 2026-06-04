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
    public ProcessingOverallStatus OverallStatus { get; set; } = ProcessingOverallStatus.Pending;
    public PaperProcessingStep CurrentStep { get; set; } = PaperProcessingStep.Upload;
    public int ProgressPercent { get; set; }

    public ProcessingStepStatus UploadStatus { get; set; } = ProcessingStepStatus.NotStarted;
    public ProcessingStepStatus MarkdownStatus { get; set; } = ProcessingStepStatus.NotStarted;
    public ProcessingStepStatus MetadataExtractionStatus { get; set; } = ProcessingStepStatus.NotStarted;
    public ProcessingStepStatus MetadataQualityStatus { get; set; } = ProcessingStepStatus.NotStarted;
    public ProcessingStepStatus OpenAlexStatus { get; set; } = ProcessingStepStatus.NotStarted;
    public ProcessingStepStatus CrossrefStatus { get; set; } = ProcessingStepStatus.NotStarted;
    public ProcessingStepStatus AiReviewStatus { get; set; } = ProcessingStepStatus.NotStarted;
    public ProcessingStepStatus IntegrityScreeningStatus { get; set; } = ProcessingStepStatus.NotStarted;

    public string? PaperUploadedEventId { get; set; }
    public string? MarkdownRequestedEventId { get; set; }
    public string? MarkdownGeneratedEventId { get; set; }
    public string? MetadataExtractionRequestedEventId { get; set; }
    public string? MetadataExtractedEventId { get; set; }
    public string? MetadataQualityScoredEventId { get; set; }
    public string? OpenAlexRequestedEventId { get; set; }
    public string? OpenAlexCompletedEventId { get; set; }
    public string? OpenAlexSkippedEventId { get; set; }

    public string? LastErrorCode { get; set; }
    public string? LastErrorMessage { get; set; }
    public PaperProcessingStep? LastFailedStep { get; set; }
    public string? ErrorDetailsJson { get; set; }

    public int RetryCount { get; set; }
    public DateTime? LastRetryAt { get; set; }
    public DateTime? NextRetryAt { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? UploadCompletedAt { get; set; }
    public DateTime? MarkdownStartedAt { get; set; }
    public DateTime? MarkdownCompletedAt { get; set; }
    public DateTime? MetadataStartedAt { get; set; }
    public DateTime? MetadataCompletedAt { get; set; }
    public DateTime? MetadataQualityCompletedAt { get; set; }
    public DateTime? OpenAlexStartedAt { get; set; }
    public DateTime? OpenAlexCompletedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    public string? StepDetailsJson { get; set; }
    public string? WarningsJson { get; set; }
}
