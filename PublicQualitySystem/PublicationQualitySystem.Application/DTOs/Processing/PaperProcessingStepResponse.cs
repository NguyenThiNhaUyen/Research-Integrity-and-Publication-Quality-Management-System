using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Application.DTOs.Processing;

public class PaperProcessingStepResponse
{
    public string Name { get; set; } = string.Empty;
    public PaperProcessingStep Step { get; set; }
    public ProcessingStepStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
