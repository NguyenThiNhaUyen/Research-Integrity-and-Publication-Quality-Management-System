using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Application.DTOs.Processing;

public class PaperProcessingTrackerResponse
{
    public long PaperId { get; set; }
    public long PaperVersionId { get; set; }
    public string? CorrelationId { get; set; }
    public ProcessingStage CurrentStage { get; set; }
    public ProcessingStatus CurrentStatus { get; set; }
    public ProcessingStatus OverallStatus { get; set; }
    public int ProgressPercent { get; set; }
    public string? LastError { get; set; }
    public int RetryCount { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public IReadOnlyList<PaperProcessingStepResponse> Steps { get; set; } = Array.Empty<PaperProcessingStepResponse>();
}
