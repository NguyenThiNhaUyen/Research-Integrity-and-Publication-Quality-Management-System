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
        var now = DateTime.UtcNow;
        var tracker = await db.PaperProcessingTrackers
            .Include(x => x.Events)
            .FirstOrDefaultAsync(x => x.PaperVersionId == paperVersionId, cancellationToken);

        if (tracker is null)
        {
            tracker = new PaperProcessingTracker
            {
                PaperId = paperId,
                PaperVersionId = paperVersionId,
                CorrelationId = correlationId,
                CurrentStage = ProcessingStage.UPLOADED,
                CurrentStatus = ProcessingStatus.PENDING,
                OverallStatus = ProcessingStatus.PENDING,
                ProgressPercent = 0,
                RetryCount = 0,
                StartedAt = now,
                LastUpdatedAt = now
            };
            db.PaperProcessingTrackers.Add(tracker);
            await db.SaveChangesAsync(cancellationToken);
        }

        tracker.CurrentStage = ProcessingStage.UPLOADED;
        tracker.CurrentStatus = ProcessingStatus.PENDING;
        tracker.OverallStatus = ProcessingStatus.PENDING;
        tracker.ProgressPercent = Math.Max(tracker.ProgressPercent, ProgressFor(ProcessingStage.UPLOADED));
        tracker.LastUpdatedAt = now;

        AddEvent(
            tracker,
            eventId: correlationId,
            eventType: "UploadCreated",
            stage: ProcessingStage.UPLOADED,
            status: ProcessingStatus.PENDING,
            payloadJson: $"{{\"correlationId\":\"{Escape(correlationId)}\"}}",
            errorMessage: null);

        await db.SaveChangesAsync(cancellationToken);
        return await ToResponseAsync(tracker, cancellationToken);
    }

    public async Task<PaperProcessingTrackerResponse> UpdateTrackerStageAsync(
        long paperId,
        long paperVersionId,
        ProcessingStage stage,
        ProcessingStatus status,
        string eventType,
        string? payloadJson = null,
        string? errorMessage = null,
        string? eventId = null,
        CancellationToken cancellationToken = default)
    {
        var tracker = await db.PaperProcessingTrackers
            .Include(x => x.Events)
            .FirstOrDefaultAsync(x => x.PaperVersionId == paperVersionId, cancellationToken);

        if (tracker is null)
        {
            tracker = new PaperProcessingTracker
            {
                PaperId = paperId,
                PaperVersionId = paperVersionId,
                CorrelationId = eventId ?? Guid.NewGuid().ToString("N"),
                CurrentStage = ProcessingStage.UPLOADED,
                CurrentStatus = ProcessingStatus.PENDING,
                OverallStatus = ProcessingStatus.PENDING,
                ProgressPercent = 0,
                RetryCount = 0,
                StartedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };
            db.PaperProcessingTrackers.Add(tracker);
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(eventId) && HasEvent(tracker, eventId, eventType))
        {
            return await ToResponseAsync(tracker, cancellationToken);
        }

        ApplyTrackerStage(tracker, stage, status, eventType, payloadJson, errorMessage, eventId);
        await db.SaveChangesAsync(cancellationToken);
        return await ToResponseAsync(tracker, cancellationToken);
    }

    public async Task RecordEventPublishedAsync(
        long paperVersionId,
        ProcessingStage stage,
        string eventId,
        string eventType,
        string? payloadJson = null,
        CancellationToken cancellationToken = default)
    {
        var tracker = await GetTrackerAsync(paperVersionId, cancellationToken);
        if (HasEvent(tracker, eventId, eventType))
        {
            return;
        }

        ApplyTrackerStage(tracker, stage, ProcessingStatus.PROCESSING, eventType, payloadJson, errorMessage: null, eventId);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordStepStartedAsync(
        long paperVersionId,
        ProcessingStage stage,
        string eventType,
        string? payloadJson = null,
        CancellationToken cancellationToken = default)
    {
        var tracker = await GetTrackerAsync(paperVersionId, cancellationToken);
        ApplyTrackerStage(tracker, stage, ProcessingStatus.PROCESSING, eventType, payloadJson, errorMessage: null, eventId: null);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordStepCompletedAsync(
        long paperVersionId,
        ProcessingStage stage,
        string eventType,
        string? detailsJson = null,
        CancellationToken cancellationToken = default)
    {
        var tracker = await GetTrackerAsync(paperVersionId, cancellationToken);
        ApplyTrackerStage(tracker, stage, ProcessingStatus.COMPLETED, eventType, detailsJson, errorMessage: null, eventId: null);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordStepFailedAsync(
        long paperVersionId,
        ProcessingStage stage,
        string eventType,
        string errorMessage,
        string? errorDetailsJson = null,
        CancellationToken cancellationToken = default)
    {
        var tracker = await GetTrackerAsync(paperVersionId, cancellationToken);
        var sanitizedError = Sanitize(errorMessage, 1000);

        ApplyTrackerStage(tracker, stage, ProcessingStatus.FAILED, eventType, errorDetailsJson, sanitizedError, eventId: null);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordStepSkippedAsync(
        long paperVersionId,
        ProcessingStage stage,
        string eventType,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var tracker = await GetTrackerAsync(paperVersionId, cancellationToken);
        var payload = $"{{\"reason\":\"{Escape(reason)}\"}}";
        ApplyTrackerStage(tracker, stage, ProcessingStatus.COMPLETED, eventType, payload, errorMessage: null, eventId: null);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ScheduleRetryAsync(
        long paperVersionId,
        ProcessingStage stage,
        int retryCount,
        DateTime nextRetryAt,
        CancellationToken cancellationToken = default)
    {
        var tracker = await GetTrackerAsync(paperVersionId, cancellationToken);
        tracker.CurrentStage = stage;
        tracker.CurrentStatus = ProcessingStatus.PROCESSING;
        tracker.OverallStatus = ProcessingStatus.PROCESSING;
        tracker.RetryCount = retryCount;
        tracker.LastUpdatedAt = DateTime.UtcNow;
        AddEvent(
            tracker,
            eventId: null,
            eventType: "RetryScheduled",
            stage,
            ProcessingStatus.PROCESSING,
            $"{{\"retryCount\":{retryCount},\"nextRetryAt\":\"{nextRetryAt:O}\"}}",
            errorMessage: null);
        await db.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTrackerStage(
        PaperProcessingTracker tracker,
        ProcessingStage stage,
        ProcessingStatus status,
        string eventType,
        string? payloadJson,
        string? errorMessage,
        string? eventId)
    {
        if (status == ProcessingStatus.FAILED)
        {
            tracker.CurrentStage = ProcessingStage.FAILED;
            tracker.CurrentStatus = ProcessingStatus.FAILED;
            tracker.OverallStatus = ProcessingStatus.FAILED;
            tracker.LastError = Sanitize(errorMessage ?? payloadJson, 1000);
            tracker.LastUpdatedAt = DateTime.UtcNow;
            tracker.ProgressPercent = Math.Max(tracker.ProgressPercent, ProgressFor(stage));
        }
        else
        {
            UpdateSnapshot(tracker, stage, status, payloadJson);
        }

        AddEvent(tracker, eventId, eventType, stage, status, payloadJson, errorMessage);
    }

    public async Task RecalculateOverallStatusAsync(long paperVersionId, CancellationToken cancellationToken = default)
    {
        var tracker = await GetTrackerAsync(paperVersionId, cancellationToken);
        tracker.ProgressPercent = Math.Max(tracker.ProgressPercent, ProgressFor(tracker.CurrentStage));
        tracker.OverallStatus = tracker.CurrentStatus;
        tracker.LastUpdatedAt = DateTime.UtcNow;
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

        return await ToResponseAsync(tracker, cancellationToken);
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
        return await ToResponseAsync(tracker, cancellationToken);
    }

    private async Task<PaperProcessingTracker> GetTrackerAsync(long paperVersionId, CancellationToken cancellationToken)
    {
        var tracker = await db.PaperProcessingTrackers
            .Include(x => x.Events)
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

    private static bool HasEvent(PaperProcessingTracker tracker, string eventId, string eventType) =>
        tracker.Events.Any(x =>
            !string.IsNullOrWhiteSpace(x.EventId)
            && x.EventId == eventId
            && x.EventType == eventType);

    private static void UpdateSnapshot(
        PaperProcessingTracker tracker,
        ProcessingStage stage,
        ProcessingStatus status,
        string? detailsJson)
    {
        if (tracker.CurrentStatus == ProcessingStatus.COMPLETED
            && status == ProcessingStatus.PROCESSING
            && ProgressFor(stage) <= tracker.ProgressPercent)
        {
            return;
        }

        tracker.CurrentStage = stage;
        tracker.CurrentStatus = status;
        tracker.OverallStatus = status;
        tracker.ProgressPercent = Math.Max(tracker.ProgressPercent, ProgressFor(stage));
        tracker.LastUpdatedAt = DateTime.UtcNow;
        tracker.LastError = status == ProcessingStatus.FAILED ? Sanitize(detailsJson, 1000) : tracker.LastError;
        if (stage == ProcessingStage.COMPLETED && status == ProcessingStatus.COMPLETED)
        {
            tracker.CompletedAt ??= DateTime.UtcNow;
            tracker.ProgressPercent = 100;
        }
    }

    private static void AddEvent(
        PaperProcessingTracker tracker,
        string? eventId,
        string eventType,
        ProcessingStage stage,
        ProcessingStatus status,
        string? payloadJson,
        string? errorMessage)
    {
        tracker.Events.Add(new PaperProcessingEvent
        {
            PaperId = tracker.PaperId,
            PaperVersionId = tracker.PaperVersionId,
            TrackerId = tracker.Id,
            EventId = Sanitize(eventId, 100),
            EventType = Sanitize(eventType, 255) ?? string.Empty,
            Stage = stage,
            Status = status,
            PayloadJson = SafeJson(payloadJson),
            ErrorMessage = Sanitize(errorMessage, 4000)
        });
    }

    private async Task<PaperProcessingTrackerResponse> ToResponseAsync(
        PaperProcessingTracker tracker,
        CancellationToken cancellationToken)
    {
        var events = await db.Set<PaperProcessingEvent>()
            .AsNoTracking()
            .Where(x => x.TrackerId == tracker.Id)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return new PaperProcessingTrackerResponse
        {
            PaperId = tracker.PaperId,
            PaperVersionId = tracker.PaperVersionId,
            CorrelationId = tracker.CorrelationId,
            CurrentStage = tracker.CurrentStage,
            CurrentStatus = tracker.CurrentStatus,
            OverallStatus = tracker.OverallStatus,
            ProgressPercent = tracker.ProgressPercent,
            LastError = tracker.LastError,
            RetryCount = tracker.RetryCount,
            StartedAt = tracker.StartedAt,
            LastUpdatedAt = tracker.LastUpdatedAt,
            CompletedAt = tracker.CompletedAt,
            Steps = BuildSteps(events)
        };
    }

    private static IReadOnlyList<PaperProcessingStepResponse> BuildSteps(IReadOnlyList<PaperProcessingEvent> events)
    {
        return events
            .GroupBy(x => x.Stage)
            .Select(group =>
            {
                var ordered = group.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).ToArray();
                var latest = ordered[^1];
                return new PaperProcessingStepResponse
                {
                    Name = NameFor(latest.Stage),
                    Stage = latest.Stage,
                    Status = latest.Status,
                    EventId = latest.EventId,
                    EventType = latest.EventType,
                    StartedAt = ordered.FirstOrDefault(x => x.Status == ProcessingStatus.PROCESSING)?.CreatedAt,
                    CompletedAt = ordered.LastOrDefault(x => x.Status == ProcessingStatus.COMPLETED)?.CreatedAt,
                    ErrorMessage = ordered.LastOrDefault(x => x.Status == ProcessingStatus.FAILED)?.ErrorMessage,
                    PayloadJson = latest.PayloadJson
                };
            })
            .OrderBy(x => OrderFor(x.Stage))
            .ToArray();
    }

    private static string NameFor(ProcessingStage stage) => stage switch
    {
        ProcessingStage.UPLOADED => "Uploaded",
        ProcessingStage.OCR_REQUESTED => "OCR Requested",
        ProcessingStage.OCR_COMPLETED => "OCR Completed",
        ProcessingStage.METADATA_REQUESTED => "Metadata Requested",
        ProcessingStage.METADATA_COMPLETED => "Metadata Completed",
        ProcessingStage.QUALITY_SCORING_REQUESTED => "Quality Scoring Requested",
        ProcessingStage.QUALITY_SCORING_COMPLETED => "Quality Scoring Completed",
        ProcessingStage.OPENALEX_REQUESTED => "OpenAlex Requested",
        ProcessingStage.OPENALEX_COMPLETED => "OpenAlex Completed",
        ProcessingStage.COMPLETED => "Completed",
        ProcessingStage.FAILED => "Failed",
        _ => stage.ToString()
    };

    private static int OrderFor(ProcessingStage stage) => stage switch
    {
        ProcessingStage.UPLOADED => 0,
        ProcessingStage.OCR_REQUESTED => 10,
        ProcessingStage.OCR_COMPLETED => 20,
        ProcessingStage.METADATA_REQUESTED => 30,
        ProcessingStage.METADATA_COMPLETED => 40,
        ProcessingStage.QUALITY_SCORING_REQUESTED => 50,
        ProcessingStage.QUALITY_SCORING_COMPLETED => 60,
        ProcessingStage.OPENALEX_REQUESTED => 70,
        ProcessingStage.OPENALEX_COMPLETED => 80,
        ProcessingStage.COMPLETED => 90,
        ProcessingStage.FAILED => 100,
        _ => 999
    };

    private static int ProgressFor(ProcessingStage stage) => stage switch
    {
        ProcessingStage.UPLOADED => 10,
        ProcessingStage.OCR_COMPLETED => 35,
        ProcessingStage.METADATA_COMPLETED => 60,
        ProcessingStage.QUALITY_SCORING_COMPLETED => 75,
        ProcessingStage.OPENALEX_COMPLETED => 85,
        ProcessingStage.COMPLETED => 100,
        _ => 0
    };

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
}
