using PublicationQualitySystem.Application.DTOs.Audit;
using PublicationQualitySystem.Domain.Entities;

namespace PublicationQualitySystem.Application.Mappings;

public static class AuditLogMapper
{
    public static AuditLogResponse ToResponse(AuditLog log) => new()
    {
        Id = log.Id,
        PaperId = log.PaperId,
        PaperVersionId = log.PaperVersionId,
        UploadedFileId = log.UploadedFileId,
        UserId = log.UserId,
        CorrelationId = log.CorrelationId,
        Action = log.Action,
        Step = log.Step,
        Status = log.Status,
        Message = log.Message,
        ErrorMessage = log.ErrorMessage,
        MetadataJson = log.MetadataJson,
        StartedAt = log.StartedAt,
        CompletedAt = log.CompletedAt,
        CreatedAt = log.CreatedAt
    };
}
