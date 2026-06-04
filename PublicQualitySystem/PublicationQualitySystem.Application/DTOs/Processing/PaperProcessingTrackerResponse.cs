using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Application.DTOs.Processing;

public class PaperProcessingTrackerResponse
{
    public long PaperId { get; set; }
    public long PaperVersionId { get; set; }
    public string? CorrelationId { get; set; }
    public ProcessingOverallStatus OverallStatus { get; set; }
    public PaperProcessingStep CurrentStep { get; set; }
    public int ProgressPercent { get; set; }
    public ProcessingStepStatus UploadStatus { get; set; }
    public ProcessingStepStatus MarkdownStatus { get; set; }
    public ProcessingStepStatus MetadataExtractionStatus { get; set; }
    public ProcessingStepStatus MetadataQualityStatus { get; set; }
    public ProcessingStepStatus OpenAlexStatus { get; set; }
    public ProcessingStepStatus CrossrefStatus { get; set; }
    public ProcessingStepStatus AiReviewStatus { get; set; }
    public ProcessingStepStatus IntegrityScreeningStatus { get; set; }
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
    public string? WarningsJson { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public IReadOnlyList<PaperProcessingStepResponse> Steps { get; set; } = Array.Empty<PaperProcessingStepResponse>();
}
