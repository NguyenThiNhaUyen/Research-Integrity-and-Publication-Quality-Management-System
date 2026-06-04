using PublicationQualitySystem.Application.DTOs.Audit;
using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IAuditLogService
{
    Task<AuditLogResponse> StartStepAsync(
        ProcessingStep step,
        string action,
        long? paperId = null,
        long? paperVersionId = null,
        long? uploadedFileId = null,
        string? userId = null,
        string? correlationId = null,
        string? message = null,
        object? metadata = null,
        CancellationToken cancellationToken = default);

    Task<AuditLogResponse> CompleteStepAsync(
        ProcessingStep step,
        string action,
        long? paperId = null,
        long? paperVersionId = null,
        long? uploadedFileId = null,
        string? userId = null,
        string? correlationId = null,
        string? message = null,
        object? metadata = null,
        CancellationToken cancellationToken = default);

    Task<AuditLogResponse> FailStepAsync(
        ProcessingStep step,
        string action,
        string errorMessage,
        long? paperId = null,
        long? paperVersionId = null,
        long? uploadedFileId = null,
        string? userId = null,
        string? correlationId = null,
        string? message = null,
        object? metadata = null,
        CancellationToken cancellationToken = default);

    Task<AuditLogResponse> SkipStepAsync(
        ProcessingStep step,
        string action,
        long? paperId = null,
        long? paperVersionId = null,
        long? uploadedFileId = null,
        string? userId = null,
        string? correlationId = null,
        string? message = null,
        object? metadata = null,
        CancellationToken cancellationToken = default);

    Task<AuditLogResponse> AddLogAsync(
        ProcessingStep step,
        AuditLogStatus status,
        string action,
        long? paperId = null,
        long? paperVersionId = null,
        long? uploadedFileId = null,
        string? userId = null,
        string? correlationId = null,
        string? message = null,
        string? errorMessage = null,
        object? metadata = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLogResponse>> GetLogsByPaperIdAsync(long paperId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AuditLogResponse>> GetLogsByPaperVersionIdAsync(long paperVersionId, CancellationToken cancellationToken);

    Task<ProcessingProgressResponse> GetProcessingProgressByPaperIdAsync(long paperId, CancellationToken cancellationToken);
}
