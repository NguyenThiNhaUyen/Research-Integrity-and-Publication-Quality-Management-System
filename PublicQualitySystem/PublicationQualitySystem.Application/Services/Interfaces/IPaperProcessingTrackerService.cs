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

    Task MarkEventPublishedAsync(
        long paperVersionId,
        PaperProcessingStep step,
        string eventId,
        string topicName,
        CancellationToken cancellationToken = default);

    Task MarkStepStartedAsync(
        long paperVersionId,
        PaperProcessingStep step,
        CancellationToken cancellationToken = default);

    Task MarkStepCompletedAsync(
        long paperVersionId,
        PaperProcessingStep step,
        string? detailsJson = null,
        CancellationToken cancellationToken = default);

    Task MarkStepFailedAsync(
        long paperVersionId,
        PaperProcessingStep step,
        string? errorCode,
        string errorMessage,
        string? errorDetailsJson = null,
        CancellationToken cancellationToken = default);

    Task MarkStepSkippedAsync(
        long paperVersionId,
        PaperProcessingStep step,
        string reason,
        CancellationToken cancellationToken = default);

    Task ScheduleRetryAsync(
        long paperVersionId,
        PaperProcessingStep step,
        int retryCount,
        DateTime nextRetryAt,
        CancellationToken cancellationToken = default);

    Task RecalculateOverallStatusAsync(
        long paperVersionId,
        CancellationToken cancellationToken = default);

    Task<PaperProcessingTrackerResponse> GetByPaperIdAsync(long paperId, CancellationToken cancellationToken = default);

    Task<PaperProcessingTrackerResponse> GetByPaperVersionIdAsync(long paperVersionId, CancellationToken cancellationToken = default);
}
