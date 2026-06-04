using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Application.DTOs.Processing;

public class PaperProcessingStepResponse
{
    public string Name { get; set; } = string.Empty;
    public ProcessingStage Stage { get; set; }
    public ProcessingStatus Status { get; set; }
    public string? EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PayloadJson { get; set; }
}
