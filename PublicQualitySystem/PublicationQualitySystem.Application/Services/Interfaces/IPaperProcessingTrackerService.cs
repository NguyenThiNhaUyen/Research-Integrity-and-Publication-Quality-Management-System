using PublicationQualitySystem.Application.DTOs.Processing;
using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IPaperProcessingTrackerService
{
    Task<PaperProcessingTrackerResponse> CreateForUploadAsync(
        long paperId,
        long paperVersionId,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<PaperProcessingTrackerResponse> UpdateTrackerStageAsync(
        long paperId,
        long paperVersionId,
        ProcessingStage stage,
        ProcessingStatus status,
        string eventType,
        string? payloadJson = null,
        string? errorMessage = null,
        string? eventId = null,
        CancellationToken cancellationToken = default);

    Task RecordEventPublishedAsync(
        long paperVersionId,
        ProcessingStage stage,
        string eventId,
        string eventType,
        string? payloadJson = null,
        CancellationToken cancellationToken = default);

    Task RecordStepStartedAsync(
        long paperVersionId,
        ProcessingStage stage,
        string eventType,
        string? payloadJson = null,
        CancellationToken cancellationToken = default);

    Task RecordStepCompletedAsync(
        long paperVersionId,
        ProcessingStage stage,
        string eventType,
        string? detailsJson = null,
        CancellationToken cancellationToken = default);

    Task RecordStepFailedAsync(
        long paperVersionId,
        ProcessingStage stage,
        string eventType,
        string errorMessage,
        string? errorDetailsJson = null,
        CancellationToken cancellationToken = default);

    Task RecordStepSkippedAsync(
        long paperVersionId,
        ProcessingStage stage,
        string eventType,
        string reason,
        CancellationToken cancellationToken = default);

    Task ScheduleRetryAsync(
        long paperVersionId,
        ProcessingStage stage,
        int retryCount,
        DateTime nextRetryAt,
        CancellationToken cancellationToken = default);

    Task RecalculateOverallStatusAsync(
        long paperVersionId,
        CancellationToken cancellationToken = default);

    Task<PaperProcessingTrackerResponse> GetByPaperIdAsync(long paperId, CancellationToken cancellationToken = default);

    Task<PaperProcessingTrackerResponse> GetByPaperVersionIdAsync(long paperVersionId, CancellationToken cancellationToken = default);
}
