using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Services.Implementations;

namespace PublicationQualitySystem.Infrastructure.Tests;

public class AuditLogServiceTests
{
    [Fact]
    public void BuildProcessingProgressResponse_ComputesProgressFromCompletedAndSkippedSteps()
    {
        var logs = new[]
        {
            CreateLog(ProcessingStep.FILE_UPLOADED, AuditLogStatus.COMPLETED, 1),
            CreateLog(ProcessingStep.FILE_VALIDATED, AuditLogStatus.COMPLETED, 2),
            CreateLog(ProcessingStep.S3_UPLOADED, AuditLogStatus.COMPLETED, 3),
            CreateLog(ProcessingStep.UPLOADED_FILE_CREATED, AuditLogStatus.SKIPPED, 4)
        };

        var progress = AuditLogService.BuildProcessingProgressResponse(1, logs);

        Assert.Equal(14, progress.TotalSteps);
        Assert.Equal(3, progress.CompletedSteps);
        Assert.Equal(0, progress.FailedSteps);
        Assert.Equal(29, progress.ProgressPercent);
        Assert.Equal(ProcessingStep.PAPER_CREATED, progress.CurrentStep);
        Assert.Equal(AuditLogStatus.COMPLETED, progress.OverallStatus);
    }

    [Fact]
    public void BuildProcessingProgressResponse_UsesLatestInProgressStepAsCurrent()
    {
        var logs = new[]
        {
            CreateLog(ProcessingStep.FILE_UPLOADED, AuditLogStatus.COMPLETED, 1),
            CreateLog(ProcessingStep.GROBID_METADATA_EXTRACTION, AuditLogStatus.IN_PROGRESS, 2)
        };

        var progress = AuditLogService.BuildProcessingProgressResponse(1, logs);

        Assert.Equal(ProcessingStep.GROBID_METADATA_EXTRACTION, progress.CurrentStep);
        Assert.Equal(AuditLogStatus.IN_PROGRESS, progress.OverallStatus);
    }

    [Fact]
    public void BuildProcessingProgressResponse_FailedLogSetsOverallStatusFailed()
    {
        var logs = new[]
        {
            CreateLog(ProcessingStep.FILE_UPLOADED, AuditLogStatus.COMPLETED, 1),
            CreateLog(ProcessingStep.GROBID_METADATA_EXTRACTION, AuditLogStatus.FAILED, 2)
        };

        var progress = AuditLogService.BuildProcessingProgressResponse(1, logs);

        Assert.Equal(ProcessingStep.GROBID_METADATA_EXTRACTION, progress.CurrentStep);
        Assert.Equal(AuditLogStatus.FAILED, progress.OverallStatus);
        Assert.Equal(1, progress.FailedSteps);
    }

    private static AuditLog CreateLog(ProcessingStep step, AuditLogStatus status, int minute) => new()
    {
        Id = Guid.NewGuid(),
        PaperId = 1,
        PaperVersionId = 10,
        Step = step,
        Status = status,
        Action = step.ToString(),
        CreatedAt = new DateTime(2026, 6, 3, 0, minute, 0, DateTimeKind.Utc)
    };
}
