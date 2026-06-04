using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Application.DTOs.Processing;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Configurations;
using PublicationQualitySystem.Infrastructure.Security;
using PublicationQualitySystem.Shared.Exceptions;
using PublicationQualitySystem.Shared.Extensions;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public sealed class PaperProcessingTrackerService(
    ApplicationDbContext db,
    ICurrentUserProvider currentUser) : IPaperProcessingTrackerService
{
    public async Task<PaperProcessingTrackerResponse> CreateForUploadAsync(
        long paperId,
        long paperVersionId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var tracker = await db.PaperProcessingTrackers
            .FirstOrDefaultAsync(x => x.PaperVersionId == paperVersionId, cancellationToken);
        var now = DateTime.UtcNow;

        if (tracker is null)
        {
            tracker = new PaperProcessingTracker
            {
                PaperId = paperId,
                PaperVersionId = paperVersionId,
                CorrelationId = correlationId,
                StartedAt = now
            };
            db.PaperProcessingTrackers.Add(tracker);
        }

        tracker.OverallStatus = ProcessingOverallStatus.Processing;
        tracker.CurrentStep = PaperProcessingStep.Upload;
        tracker.UploadStatus = ProcessingStepStatus.Completed;
        tracker.ProgressPercent = Math.Max(tracker.ProgressPercent, 10);
        tracker.UploadCompletedAt ??= now;
        tracker.LastUpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(tracker);
    }

    public async Task MarkEventPublishedAsync(
        long paperVersionId,
        PaperProcessingStep step,
        string eventId,
        string topicName,
        CancellationToken cancellationToken = default)
    {
        var tracker = await GetTrackerAsync(paperVersionId, cancellationToken);
        SetEventIdIfEmptyOrSame(tracker, step, eventId, topicName);
        SetStatusIfNotCompleted(tracker, step, ProcessingStepStatus.EventPublished);
        tracker.CurrentStep = step;
        tracker.StepDetailsJson = SafeJson($"{{\"lastEventId\":\"{Escape(eventId)}\",\"topic\":\"{Escape(topicName)}\"}}");
        Touch(tracker);
        Recalculate(tracker);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkStepStartedAsync(long paperVersionId, PaperProcessingStep step, CancellationToken cancellationToken = default)
    {
        var tracker = await GetTrackerAsync(paperVersionId, cancellationToken);
        if (GetStatus(tracker, step) == ProcessingStepStatus.Completed)
        {
            return;
        }

        SetStatus(tracker, step, ProcessingStepStatus.Processing);
        SetStartedAt(tracker, step, DateTime.UtcNow);
        tracker.CurrentStep = step;
        Touch(tracker);
        Recalculate(tracker);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkStepCompletedAsync(
        long paperVersionId,
        PaperProcessingStep step,
        string? detailsJson = null,
        CancellationToken cancellationToken = default)
    {
        var tracker = await GetTrackerAsync(paperVersionId, cancellationToken);
        SetStatus(tracker, step, ProcessingStepStatus.Completed);
        SetCompletedAt(tracker, step, DateTime.UtcNow);
        tracker.ProgressPercent = Math.Max(tracker.ProgressPercent, ProgressFor(step, ProcessingStepStatus.Completed));
        tracker.StepDetailsJson = SafeJson(detailsJson);
        tracker.CurrentStep = ResolveNextStep(step);
        Touch(tracker);
        Recalculate(tracker);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkStepFailedAsync(
        long paperVersionId,
        PaperProcessingStep step,
        string? errorCode,
        string errorMessage,
        string? errorDetailsJson = null,
        CancellationToken cancellationToken = default)
    {
        var tracker = await GetTrackerAsync(paperVersionId, cancellationToken);
        SetStatus(tracker, step, ProcessingStepStatus.Failed);
        tracker.LastFailedStep = step;
        tracker.LastErrorCode = errorCode;
        tracker.LastErrorMessage = Sanitize(errorMessage, 1000);
        tracker.ErrorDetailsJson = SafeJson(errorDetailsJson);
        tracker.FailedAt = DateTime.UtcNow;
        tracker.CurrentStep = step;
        Touch(tracker);
        Recalculate(tracker);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkStepSkippedAsync(
        long paperVersionId,
        PaperProcessingStep step,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var tracker = await GetTrackerAsync(paperVersionId, cancellationToken);
        if (GetStatus(tracker, step) != ProcessingStepStatus.Completed)
        {
            SetStatus(tracker, step, ProcessingStepStatus.Skipped);
        }

        SetCompletedAt(tracker, step, DateTime.UtcNow);
        tracker.ProgressPercent = Math.Max(tracker.ProgressPercent, ProgressFor(step, ProcessingStepStatus.Skipped));
        tracker.WarningsJson = SafeJson($"[\"{Escape(reason)}\"]");
        tracker.CurrentStep = ResolveNextStep(step);
        Touch(tracker);
        Recalculate(tracker);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ScheduleRetryAsync(
        long paperVersionId,
        PaperProcessingStep step,
        int retryCount,
        DateTime nextRetryAt,
        CancellationToken cancellationToken = default)
    {
        var tracker = await GetTrackerAsync(paperVersionId, cancellationToken);
        SetStatusIfNotCompleted(tracker, step, ProcessingStepStatus.RetryScheduled);
        tracker.RetryCount = retryCount;
        tracker.LastRetryAt = DateTime.UtcNow;
        tracker.NextRetryAt = nextRetryAt;
        tracker.CurrentStep = step;
        Touch(tracker);
        Recalculate(tracker);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RecalculateOverallStatusAsync(long paperVersionId, CancellationToken cancellationToken = default)
    {
        var tracker = await GetTrackerAsync(paperVersionId, cancellationToken);
        Recalculate(tracker);
        Touch(tracker);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PaperProcessingTrackerResponse> GetByPaperIdAsync(long paperId, CancellationToken cancellationToken = default)
    {
        await EnsureCanViewPaperAsync(paperId, cancellationToken);
        var tracker = await db.PaperProcessingTrackers
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.PaperId == paperId, cancellationToken);
        if (tracker is null)
        {
            throw new AppException(AuditLogErrorCode.NotFound);
        }

        return ToResponse(tracker);
    }

    public async Task<PaperProcessingTrackerResponse> GetByPaperVersionIdAsync(long paperVersionId, CancellationToken cancellationToken = default)
    {
        var tracker = await db.PaperProcessingTrackers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PaperVersionId == paperVersionId, cancellationToken);
        if (tracker is null)
        {
            throw new AppException(AuditLogErrorCode.NotFound);
        }

        await EnsureCanViewPaperAsync(tracker.PaperId, cancellationToken);
        return ToResponse(tracker);
    }

    private async Task<PaperProcessingTracker> GetTrackerAsync(long paperVersionId, CancellationToken cancellationToken)
    {
        var tracker = await db.PaperProcessingTrackers
            .FirstOrDefaultAsync(x => x.PaperVersionId == paperVersionId, cancellationToken);
        if (tracker is null)
        {
            throw new AppException(AuditLogErrorCode.NotFound);
        }

        return tracker;
    }

    private async Task EnsureCanViewPaperAsync(long paperId, CancellationToken cancellationToken)
    {
        var paper = await db.Papers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == paperId, cancellationToken);
        if (paper is null)
        {
            throw new AppException(AuditLogErrorCode.NotFound);
        }

        if (currentUser.User.HasPermission(nameof(PermissionName.AUDIT_LOG_READ))
            || currentUser.User.HasPermission(nameof(PermissionName.PAPER_READ_ALL)))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(currentUser.Subject)
            && !string.IsNullOrWhiteSpace(paper.CreatedBy)
            && string.Equals(paper.CreatedBy, currentUser.Subject, StringComparison.Ordinal))
        {
            return;
        }

        throw new AppException(AuditLogErrorCode.Forbidden);
    }

    private static void Recalculate(PaperProcessingTracker tracker)
    {
        if (tracker.UploadStatus == ProcessingStepStatus.Failed
            || tracker.MetadataExtractionStatus == ProcessingStepStatus.Failed)
        {
            tracker.OverallStatus = ProcessingOverallStatus.Failed;
            return;
        }

        if (tracker.MarkdownStatus == ProcessingStepStatus.Failed
            || tracker.CrossrefStatus == ProcessingStepStatus.Failed
            || tracker.OpenAlexStatus == ProcessingStepStatus.Failed
            || tracker.AiReviewStatus == ProcessingStepStatus.Failed
            || tracker.IntegrityScreeningStatus == ProcessingStepStatus.Failed)
        {
            tracker.OverallStatus = ProcessingOverallStatus.PartiallyCompleted;
            return;
        }

        if (tracker.UploadStatus == ProcessingStepStatus.Completed
            && tracker.MetadataExtractionStatus == ProcessingStepStatus.Completed
            && tracker.MetadataQualityStatus == ProcessingStepStatus.Completed
            && tracker.OpenAlexStatus is ProcessingStepStatus.Completed or ProcessingStepStatus.Skipped)
        {
            tracker.OverallStatus = ProcessingOverallStatus.Completed;
            tracker.CompletedAt ??= DateTime.UtcNow;
            tracker.ProgressPercent = Math.Max(tracker.ProgressPercent, 100);
            return;
        }

        tracker.OverallStatus = tracker.UploadStatus == ProcessingStepStatus.NotStarted
            ? ProcessingOverallStatus.Pending
            : ProcessingOverallStatus.Processing;
    }

    private static void SetEventIdIfEmptyOrSame(PaperProcessingTracker tracker, PaperProcessingStep step, string eventId, string topicName)
    {
        switch (step)
        {
            case PaperProcessingStep.Upload:
                tracker.PaperUploadedEventId ??= eventId;
                break;
            case PaperProcessingStep.MarkdownConversion:
                tracker.MarkdownRequestedEventId ??= eventId;
                break;
            case PaperProcessingStep.MetadataExtraction:
                tracker.MetadataExtractionRequestedEventId ??= eventId;
                break;
            case PaperProcessingStep.MetadataQualityScoring:
                tracker.MetadataQualityScoredEventId ??= eventId;
                break;
            case PaperProcessingStep.OpenAlexSimilarityCheck:
                if (topicName.Contains("skipped", StringComparison.OrdinalIgnoreCase))
                {
                    tracker.OpenAlexSkippedEventId ??= eventId;
                }
                else
                {
                    tracker.OpenAlexRequestedEventId ??= eventId;
                }
                break;
            default:
                break;
        }
    }

    private static ProcessingStepStatus GetStatus(PaperProcessingTracker tracker, PaperProcessingStep step) => step switch
    {
        PaperProcessingStep.Upload => tracker.UploadStatus,
        PaperProcessingStep.MarkdownConversion => tracker.MarkdownStatus,
        PaperProcessingStep.MetadataExtraction => tracker.MetadataExtractionStatus,
        PaperProcessingStep.CrossrefEnrichment => tracker.CrossrefStatus,
        PaperProcessingStep.MetadataQualityScoring => tracker.MetadataQualityStatus,
        PaperProcessingStep.OpenAlexSimilarityCheck => tracker.OpenAlexStatus,
        PaperProcessingStep.AiPublicationQualityReview => tracker.AiReviewStatus,
        PaperProcessingStep.IntegrityScreening => tracker.IntegrityScreeningStatus,
        _ => ProcessingStepStatus.NotStarted
    };

    private static void SetStatusIfNotCompleted(PaperProcessingTracker tracker, PaperProcessingStep step, ProcessingStepStatus status)
    {
        if (GetStatus(tracker, step) is not (ProcessingStepStatus.Completed or ProcessingStepStatus.Skipped or ProcessingStepStatus.Failed))
        {
            SetStatus(tracker, step, status);
        }
    }

    private static void SetStatus(PaperProcessingTracker tracker, PaperProcessingStep step, ProcessingStepStatus status)
    {
        switch (step)
        {
            case PaperProcessingStep.Upload:
                tracker.UploadStatus = status;
                break;
            case PaperProcessingStep.MarkdownConversion:
                tracker.MarkdownStatus = status;
                break;
            case PaperProcessingStep.MetadataExtraction:
                tracker.MetadataExtractionStatus = status;
                break;
            case PaperProcessingStep.CrossrefEnrichment:
                tracker.CrossrefStatus = status;
                break;
            case PaperProcessingStep.MetadataQualityScoring:
                tracker.MetadataQualityStatus = status;
                break;
            case PaperProcessingStep.OpenAlexSimilarityCheck:
                tracker.OpenAlexStatus = status;
                break;
            case PaperProcessingStep.AiPublicationQualityReview:
                tracker.AiReviewStatus = status;
                break;
            case PaperProcessingStep.IntegrityScreening:
                tracker.IntegrityScreeningStatus = status;
                break;
        }
    }

    private static void SetStartedAt(PaperProcessingTracker tracker, PaperProcessingStep step, DateTime now)
    {
        switch (step)
        {
            case PaperProcessingStep.MarkdownConversion:
                tracker.MarkdownStartedAt ??= now;
                break;
            case PaperProcessingStep.MetadataExtraction:
                tracker.MetadataStartedAt ??= now;
                break;
            case PaperProcessingStep.OpenAlexSimilarityCheck:
                tracker.OpenAlexStartedAt ??= now;
                break;
        }
    }

    private static void SetCompletedAt(PaperProcessingTracker tracker, PaperProcessingStep step, DateTime now)
    {
        switch (step)
        {
            case PaperProcessingStep.Upload:
                tracker.UploadCompletedAt ??= now;
                break;
            case PaperProcessingStep.MarkdownConversion:
                tracker.MarkdownCompletedAt ??= now;
                break;
            case PaperProcessingStep.MetadataExtraction:
                tracker.MetadataCompletedAt ??= now;
                break;
            case PaperProcessingStep.MetadataQualityScoring:
                tracker.MetadataQualityCompletedAt ??= now;
                break;
            case PaperProcessingStep.OpenAlexSimilarityCheck:
                tracker.OpenAlexCompletedAt ??= now;
                break;
        }
    }

    private static PaperProcessingStep ResolveNextStep(PaperProcessingStep step) => step switch
    {
        PaperProcessingStep.Upload => PaperProcessingStep.MarkdownConversion,
        PaperProcessingStep.MarkdownConversion => PaperProcessingStep.MetadataExtraction,
        PaperProcessingStep.MetadataExtraction => PaperProcessingStep.MetadataQualityScoring,
        PaperProcessingStep.MetadataQualityScoring => PaperProcessingStep.OpenAlexSimilarityCheck,
        PaperProcessingStep.OpenAlexSimilarityCheck => PaperProcessingStep.AiPublicationQualityReview,
        PaperProcessingStep.AiPublicationQualityReview => PaperProcessingStep.IntegrityScreening,
        _ => step
    };

    private static int ProgressFor(PaperProcessingStep step, ProcessingStepStatus status) => step switch
    {
        PaperProcessingStep.Upload => 10,
        PaperProcessingStep.MarkdownConversion => 35,
        PaperProcessingStep.MetadataExtraction => 60,
        PaperProcessingStep.MetadataQualityScoring => 75,
        PaperProcessingStep.OpenAlexSimilarityCheck when status is ProcessingStepStatus.Completed or ProcessingStepStatus.Skipped => 85,
        PaperProcessingStep.AiPublicationQualityReview when status is ProcessingStepStatus.Completed or ProcessingStepStatus.Skipped => 95,
        _ => 0
    };

    private static void Touch(PaperProcessingTracker tracker) => tracker.LastUpdatedAt = DateTime.UtcNow;

    private static string? SafeJson(string? value) => Sanitize(value, 4000);

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string? Sanitize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var firstLine = value.Split(["\r\n", "\n"], StringSplitOptions.None).FirstOrDefault() ?? value;
        return firstLine.Length > maxLength ? firstLine[..maxLength] : firstLine;
    }

    private static PaperProcessingTrackerResponse ToResponse(PaperProcessingTracker tracker) => new()
    {
        PaperId = tracker.PaperId,
        PaperVersionId = tracker.PaperVersionId,
        CorrelationId = tracker.CorrelationId,
        OverallStatus = tracker.OverallStatus,
        CurrentStep = tracker.CurrentStep,
        ProgressPercent = tracker.ProgressPercent,
        UploadStatus = tracker.UploadStatus,
        MarkdownStatus = tracker.MarkdownStatus,
        MetadataExtractionStatus = tracker.MetadataExtractionStatus,
        MetadataQualityStatus = tracker.MetadataQualityStatus,
        OpenAlexStatus = tracker.OpenAlexStatus,
        CrossrefStatus = tracker.CrossrefStatus,
        AiReviewStatus = tracker.AiReviewStatus,
        IntegrityScreeningStatus = tracker.IntegrityScreeningStatus,
        PaperUploadedEventId = tracker.PaperUploadedEventId,
        MarkdownRequestedEventId = tracker.MarkdownRequestedEventId,
        MarkdownGeneratedEventId = tracker.MarkdownGeneratedEventId,
        MetadataExtractionRequestedEventId = tracker.MetadataExtractionRequestedEventId,
        MetadataExtractedEventId = tracker.MetadataExtractedEventId,
        MetadataQualityScoredEventId = tracker.MetadataQualityScoredEventId,
        OpenAlexRequestedEventId = tracker.OpenAlexRequestedEventId,
        OpenAlexCompletedEventId = tracker.OpenAlexCompletedEventId,
        OpenAlexSkippedEventId = tracker.OpenAlexSkippedEventId,
        LastErrorCode = tracker.LastErrorCode,
        LastErrorMessage = tracker.LastErrorMessage,
        LastFailedStep = tracker.LastFailedStep,
        WarningsJson = tracker.WarningsJson,
        LastUpdatedAt = tracker.LastUpdatedAt,
        Steps =
        [
            BuildStep("Upload", PaperProcessingStep.Upload, tracker.UploadStatus, tracker.StartedAt, tracker.UploadCompletedAt, tracker),
            BuildStep("Markdown Conversion", PaperProcessingStep.MarkdownConversion, tracker.MarkdownStatus, tracker.MarkdownStartedAt, tracker.MarkdownCompletedAt, tracker),
            BuildStep("Metadata Extraction", PaperProcessingStep.MetadataExtraction, tracker.MetadataExtractionStatus, tracker.MetadataStartedAt, tracker.MetadataCompletedAt, tracker),
            BuildStep("Metadata Quality Scoring", PaperProcessingStep.MetadataQualityScoring, tracker.MetadataQualityStatus, null, tracker.MetadataQualityCompletedAt, tracker),
            BuildStep("OpenAlex Similarity Check", PaperProcessingStep.OpenAlexSimilarityCheck, tracker.OpenAlexStatus, tracker.OpenAlexStartedAt, tracker.OpenAlexCompletedAt, tracker),
            BuildStep("AI Publication Quality Review", PaperProcessingStep.AiPublicationQualityReview, tracker.AiReviewStatus, null, null, tracker),
            BuildStep("Integrity Screening", PaperProcessingStep.IntegrityScreening, tracker.IntegrityScreeningStatus, null, null, tracker)
        ]
    };

    private static PaperProcessingStepResponse BuildStep(
        string name,
        PaperProcessingStep step,
        ProcessingStepStatus status,
        DateTime? startedAt,
        DateTime? completedAt,
        PaperProcessingTracker tracker) => new()
    {
        Name = name,
        Step = step,
        Status = status,
        StartedAt = startedAt,
        CompletedAt = completedAt,
        ErrorMessage = tracker.LastFailedStep == step ? tracker.LastErrorMessage : null
    };
}
