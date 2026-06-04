using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Application.DTOs.Audit;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Configurations;
using PublicationQualitySystem.Infrastructure.Security;
using PublicationQualitySystem.Shared.Exceptions;
using PublicationQualitySystem.Shared.Extensions;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public sealed class AuditLogService(
    ApplicationDbContext db,
    ICurrentUserProvider currentUser) : IAuditLogService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly ProcessingStep[] OrderedSteps =
    [
        ProcessingStep.FILE_UPLOADED,
        ProcessingStep.FILE_VALIDATED,
        ProcessingStep.S3_UPLOADED,
        ProcessingStep.UPLOADED_FILE_CREATED,
        ProcessingStep.PAPER_CREATED,
        ProcessingStep.PAPER_VERSION_CREATED,
        ProcessingStep.GROBID_METADATA_EXTRACTION,
        ProcessingStep.NOUGAT_MARKDOWN_CONVERSION,
        ProcessingStep.SECTION_EXTRACTION,
        ProcessingStep.AI_QUALITY_REVIEW,
        ProcessingStep.METADATA_QUALITY_SCORING,
        ProcessingStep.INTEGRITY_SCREENING,
        ProcessingStep.RISK_CLASSIFICATION,
        ProcessingStep.REPORT_GENERATION
    ];

    public Task<AuditLogResponse> StartStepAsync(
        ProcessingStep step,
        string action,
        long? paperId = null,
        long? paperVersionId = null,
        long? uploadedFileId = null,
        string? userId = null,
        string? correlationId = null,
        string? message = null,
        object? metadata = null,
        CancellationToken cancellationToken = default) =>
        AddLogAsync(step, AuditLogStatus.IN_PROGRESS, action, paperId, paperVersionId, uploadedFileId, userId, correlationId, message, null, metadata, cancellationToken);

    public Task<AuditLogResponse> CompleteStepAsync(
        ProcessingStep step,
        string action,
        long? paperId = null,
        long? paperVersionId = null,
        long? uploadedFileId = null,
        string? userId = null,
        string? correlationId = null,
        string? message = null,
        object? metadata = null,
        CancellationToken cancellationToken = default) =>
        AddLogAsync(step, AuditLogStatus.COMPLETED, action, paperId, paperVersionId, uploadedFileId, userId, correlationId, message, null, metadata, cancellationToken);

    public Task<AuditLogResponse> FailStepAsync(
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
        CancellationToken cancellationToken = default) =>
        AddLogAsync(step, AuditLogStatus.FAILED, action, paperId, paperVersionId, uploadedFileId, userId, correlationId, message, errorMessage, metadata, cancellationToken);

    public Task<AuditLogResponse> SkipStepAsync(
        ProcessingStep step,
        string action,
        long? paperId = null,
        long? paperVersionId = null,
        long? uploadedFileId = null,
        string? userId = null,
        string? correlationId = null,
        string? message = null,
        object? metadata = null,
        CancellationToken cancellationToken = default) =>
        AddLogAsync(step, AuditLogStatus.SKIPPED, action, paperId, paperVersionId, uploadedFileId, userId, correlationId, message, null, metadata, cancellationToken);

    public async Task<AuditLogResponse> AddLogAsync(
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
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            PaperId = paperId,
            PaperVersionId = paperVersionId,
            UploadedFileId = uploadedFileId,
            UserId = userId ?? currentUser.Subject,
            CorrelationId = correlationId,
            Action = action,
            Step = step,
            Status = status,
            Message = message,
            ErrorMessage = SanitizeError(errorMessage),
            MetadataJson = SerializeMetadata(metadata),
            StartedAt = status is AuditLogStatus.IN_PROGRESS or AuditLogStatus.COMPLETED or AuditLogStatus.FAILED ? now : null,
            CompletedAt = status is AuditLogStatus.COMPLETED or AuditLogStatus.FAILED or AuditLogStatus.SKIPPED ? now : null,
            CreatedBy = userId ?? currentUser.Subject
        };

        db.AuditLogs.Add(log);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(log);
    }

    public async Task<IReadOnlyList<AuditLogResponse>> GetLogsByPaperIdAsync(long paperId, CancellationToken cancellationToken)
    {
        await EnsureCanViewPaperAsync(paperId, cancellationToken);
        var logs = await db.AuditLogs
            .AsNoTracking()
            .Where(x => x.PaperId == paperId)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Step)
            .ToListAsync(cancellationToken);

        return logs.Select(ToResponse).ToArray();
    }

    public async Task<IReadOnlyList<AuditLogResponse>> GetLogsByPaperVersionIdAsync(long paperVersionId, CancellationToken cancellationToken)
    {
        var version = await db.PaperVersions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == paperVersionId, cancellationToken);
        if (version is null)
        {
            throw new AppException(AuditLogErrorCode.NotFound);
        }

        await EnsureCanViewPaperAsync(version.PaperId, cancellationToken);
        var logs = await db.AuditLogs
            .AsNoTracking()
            .Where(x => x.PaperVersionId == paperVersionId)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Step)
            .ToListAsync(cancellationToken);

        return logs.Select(ToResponse).ToArray();
    }

    public async Task<ProcessingProgressResponse> GetProcessingProgressByPaperIdAsync(long paperId, CancellationToken cancellationToken)
    {
        await EnsureCanViewPaperAsync(paperId, cancellationToken);
        var logs = await db.AuditLogs
            .AsNoTracking()
            .Where(x => x.PaperId == paperId)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Step)
            .ToListAsync(cancellationToken);

        return BuildProcessingProgressResponse(paperId, logs);
    }

    public static ProcessingProgressResponse BuildProcessingProgressResponse(long paperId, IReadOnlyList<AuditLog> logs)
    {
        var orderedLogs = logs
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Step)
            .ToArray();

        var latestByStep = orderedLogs
            .GroupBy(x => x.Step)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(log => log.CreatedAt).First());

        var failedSteps = latestByStep.Values.Count(x => x.Status == AuditLogStatus.FAILED);
        var completedSteps = latestByStep.Values.Count(x => x.Status == AuditLogStatus.COMPLETED);
        var progressedSteps = latestByStep.Values.Count(x => x.Status is AuditLogStatus.COMPLETED or AuditLogStatus.SKIPPED);
        var current = ResolveCurrentStep(latestByStep);
        var overallStatus = ResolveOverallStatus(latestByStep.Values);

        return new ProcessingProgressResponse
        {
            PaperId = paperId,
            PaperVersionId = orderedLogs.LastOrDefault(x => x.PaperVersionId is not null)?.PaperVersionId,
            CurrentStep = current,
            OverallStatus = overallStatus,
            TotalSteps = OrderedSteps.Length,
            CompletedSteps = completedSteps,
            FailedSteps = failedSteps,
            ProgressPercent = (int)Math.Round(progressedSteps * 100.0 / OrderedSteps.Length),
            LastUpdatedAt = orderedLogs.LastOrDefault()?.CreatedAt,
            Logs = orderedLogs.Select(ToResponse).ToArray()
        };
    }

    private async Task EnsureCanViewPaperAsync(long paperId, CancellationToken cancellationToken)
    {
        var paper = await db.Papers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == paperId, cancellationToken);
        if (paper is null)
        {
            throw new AppException(AuditLogErrorCode.NotFound);
        }

        if (CanReadAll())
        {
            return;
        }

        var subject = currentUser.Subject;
        if (!string.IsNullOrWhiteSpace(subject)
            && !string.IsNullOrWhiteSpace(paper.CreatedBy)
            && string.Equals(paper.CreatedBy, subject, StringComparison.Ordinal))
        {
            return;
        }

        throw new AppException(AuditLogErrorCode.Forbidden);
    }

    private bool CanReadAll() =>
        currentUser.User.HasPermission(nameof(PermissionName.AUDIT_LOG_READ))
        || currentUser.User.HasPermission(nameof(PermissionName.PAPER_READ_ALL));

    private static ProcessingStep? ResolveCurrentStep(IReadOnlyDictionary<ProcessingStep, AuditLog> latestByStep)
    {
        var failed = latestByStep.Values
            .Where(x => x.Status == AuditLogStatus.FAILED)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();
        if (failed is not null)
        {
            return failed.Step;
        }

        var inProgress = latestByStep.Values
            .Where(x => x.Status == AuditLogStatus.IN_PROGRESS)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();
        if (inProgress is not null)
        {
            return inProgress.Step;
        }

        foreach (var step in OrderedSteps)
        {
            if (!latestByStep.TryGetValue(step, out var log)
                || log.Status is not (AuditLogStatus.COMPLETED or AuditLogStatus.SKIPPED))
            {
                return step;
            }
        }

        return null;
    }

    private static AuditLogStatus ResolveOverallStatus(IEnumerable<AuditLog> latestLogs)
    {
        var logs = latestLogs.ToArray();
        if (logs.Any(x => x.Status == AuditLogStatus.FAILED))
        {
            return AuditLogStatus.FAILED;
        }

        if (logs.Any(x => x.Status == AuditLogStatus.IN_PROGRESS))
        {
            return AuditLogStatus.IN_PROGRESS;
        }

        return logs.Length > 0 && logs.All(x => x.Status is AuditLogStatus.COMPLETED or AuditLogStatus.SKIPPED)
            ? AuditLogStatus.COMPLETED
            : AuditLogStatus.PENDING;
    }

    private static AuditLogResponse ToResponse(AuditLog log) => new()
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

    private static string? SerializeMetadata(object? metadata) =>
        metadata is null ? null : JsonSerializer.Serialize(metadata, JsonOptions);

    private static string? SanitizeError(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return null;
        }

        var firstLine = error.Split(["\r\n", "\n"], StringSplitOptions.None).FirstOrDefault();
        return firstLine?.Length > 4000 ? firstLine[..4000] : firstLine;
    }
}
