using System.Reflection;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Services.Implementations;

namespace PublicationQualitySystem.Infrastructure.Tests;

public class PaperProcessingTrackerServiceTests
{
    [Fact]
    public void Recalculate_MetadataExtractionFailureSetsOverallFailed()
    {
        var tracker = new PaperProcessingTracker
        {
            UploadStatus = ProcessingStepStatus.Completed,
            MetadataExtractionStatus = ProcessingStepStatus.Failed
        };

        InvokePrivateStatic("Recalculate", tracker);

        Assert.Equal(ProcessingOverallStatus.Failed, tracker.OverallStatus);
    }

    [Fact]
    public void Recalculate_OpenAlexFailureSetsOverallPartiallyCompleted()
    {
        var tracker = new PaperProcessingTracker
        {
            UploadStatus = ProcessingStepStatus.Completed,
            MetadataExtractionStatus = ProcessingStepStatus.Completed,
            MetadataQualityStatus = ProcessingStepStatus.Completed,
            OpenAlexStatus = ProcessingStepStatus.Failed
        };

        InvokePrivateStatic("Recalculate", tracker);

        Assert.Equal(ProcessingOverallStatus.PartiallyCompleted, tracker.OverallStatus);
    }

    [Fact]
    public void Recalculate_OpenAlexSkippedCanCompleteWorkflow()
    {
        var tracker = new PaperProcessingTracker
        {
            UploadStatus = ProcessingStepStatus.Completed,
            MetadataExtractionStatus = ProcessingStepStatus.Completed,
            MetadataQualityStatus = ProcessingStepStatus.Completed,
            OpenAlexStatus = ProcessingStepStatus.Skipped,
            ProgressPercent = 85
        };

        InvokePrivateStatic("Recalculate", tracker);

        Assert.Equal(ProcessingOverallStatus.Completed, tracker.OverallStatus);
        Assert.Equal(100, tracker.ProgressPercent);
    }

    [Fact]
    public void SetStatusIfNotCompleted_DoesNotDowngradeCompletedStep()
    {
        var tracker = new PaperProcessingTracker
        {
            MarkdownStatus = ProcessingStepStatus.Completed
        };

        InvokePrivateStatic(
            "SetStatusIfNotCompleted",
            tracker,
            PaperProcessingStep.MarkdownConversion,
            ProcessingStepStatus.Processing);

        Assert.Equal(ProcessingStepStatus.Completed, tracker.MarkdownStatus);
    }

    private static void InvokePrivateStatic(string methodName, params object?[] parameters)
    {
        var method = typeof(PaperProcessingTrackerService).GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        method!.Invoke(null, parameters);
    }
}
