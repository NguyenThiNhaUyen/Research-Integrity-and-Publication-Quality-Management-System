using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Application.DTOs.Audit;

public class ProcessingProgressResponse
{
    public long PaperId { get; set; }
    public long? PaperVersionId { get; set; }
    public ProcessingStep? CurrentStep { get; set; }
    public AuditLogStatus OverallStatus { get; set; }
    public int TotalSteps { get; set; }
    public int CompletedSteps { get; set; }
    public int FailedSteps { get; set; }
    public int ProgressPercent { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public IReadOnlyList<AuditLogResponse> Logs { get; set; } = Array.Empty<AuditLogResponse>();
}
