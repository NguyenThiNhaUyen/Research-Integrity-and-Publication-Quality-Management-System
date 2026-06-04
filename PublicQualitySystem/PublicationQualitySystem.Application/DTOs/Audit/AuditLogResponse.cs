using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Application.DTOs.Audit;

public class AuditLogResponse
{
    public Guid Id { get; set; }
    public long? PaperId { get; set; }
    public long? PaperVersionId { get; set; }
    public long? UploadedFileId { get; set; }
    public string? UserId { get; set; }
    public string? CorrelationId { get; set; }
    public string Action { get; set; } = string.Empty;
    public ProcessingStep Step { get; set; }
    public AuditLogStatus Status { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
