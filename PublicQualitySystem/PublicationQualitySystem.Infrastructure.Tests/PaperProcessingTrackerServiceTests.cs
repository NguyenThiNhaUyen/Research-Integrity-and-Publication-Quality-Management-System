using System.Reflection;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Services.Implementations;

namespace PublicationQualitySystem.Infrastructure.Tests;

public class PaperProcessingTrackerServiceTests
{
    [Fact]
    public void PaperProcessingTracker_DefaultsToUploadedPendingPending()
    {
        var tracker = new PaperProcessingTracker();

        Assert.Equal(ProcessingStage.UPLOADED, tracker.CurrentStage);
        Assert.Equal(ProcessingStatus.PENDING, tracker.CurrentStatus);
        Assert.Equal(ProcessingStatus.PENDING, tracker.OverallStatus);
    }

    [Fact]
    public void UpdateSnapshot_CompletedStageDoesNotDowngradeToEarlierProcessingStage()
    {
        var tracker = new PaperProcessingTracker
        {
            CurrentStage = ProcessingStage.OPENALEX_COMPLETED,
            CurrentStatus = ProcessingStatus.COMPLETED,
            OverallStatus = ProcessingStatus.COMPLETED,
            ProgressPercent = 85
        };

        InvokePrivateStatic(
            "UpdateSnapshot",
            tracker,
            ProcessingStage.METADATA_REQUESTED,
            ProcessingStatus.PROCESSING,
            null);

        Assert.Equal(ProcessingStage.OPENALEX_COMPLETED, tracker.CurrentStage);
        Assert.Equal(ProcessingStatus.COMPLETED, tracker.CurrentStatus);
        Assert.Equal(ProcessingStatus.COMPLETED, tracker.OverallStatus);
        Assert.Equal(85, tracker.ProgressPercent);
    }

    [Fact]
    public void UpdateSnapshot_CompletedWorkflowSetsCompletedAtAndProgress100()
    {
        var tracker = new PaperProcessingTracker();

        InvokePrivateStatic(
            "UpdateSnapshot",
            tracker,
            ProcessingStage.COMPLETED,
            ProcessingStatus.COMPLETED,
            null);

        Assert.Equal(ProcessingStage.COMPLETED, tracker.CurrentStage);
        Assert.Equal(ProcessingStatus.COMPLETED, tracker.CurrentStatus);
        Assert.Equal(ProcessingStatus.COMPLETED, tracker.OverallStatus);
        Assert.Equal(100, tracker.ProgressPercent);
        Assert.NotNull(tracker.CompletedAt);
    }

    [Fact]
    public void UpdateSnapshot_ProcessingStageSetsOverallStatusProcessing()
    {
        var tracker = new PaperProcessingTracker();

        InvokePrivateStatic(
            "UpdateSnapshot",
            tracker,
            ProcessingStage.OCR_REQUESTED,
            ProcessingStatus.PROCESSING,
            null);

        Assert.Equal(ProcessingStage.OCR_REQUESTED, tracker.CurrentStage);
        Assert.Equal(ProcessingStatus.PROCESSING, tracker.CurrentStatus);
        Assert.Equal(ProcessingStatus.PROCESSING, tracker.OverallStatus);
    }

    [Fact]
    public void AddEvent_AppendsProcessingEventToTracker()
    {
        var tracker = new PaperProcessingTracker
        {
            Id = 10,
            PaperId = 1,
            PaperVersionId = 2
        };

        InvokePrivateStatic(
            "AddEvent",
            tracker,
            "evt-1",
            "PaperUploadedIntegrationEvent",
            ProcessingStage.UPLOADED,
            ProcessingStatus.PROCESSING,
            "{\"topic\":\"ripqms.paper-uploaded.v1\"}",
            null);

        var evt = Assert.Single(tracker.Events);
        Assert.Equal("evt-1", evt.EventId);
        Assert.Equal("PaperUploadedIntegrationEvent", evt.EventType);
        Assert.Equal(ProcessingStage.UPLOADED, evt.Stage);
        Assert.Equal(ProcessingStatus.PROCESSING, evt.Status);
        Assert.Equal(1, evt.PaperId);
        Assert.Equal(2, evt.PaperVersionId);
        Assert.Equal(10, evt.TrackerId);
    }

    [Fact]
    public void AddEvent_UploadCreatedCanRemainPending()
    {
        var tracker = new PaperProcessingTracker
        {
            Id = 10,
            PaperId = 1,
            PaperVersionId = 2
        };

        InvokePrivateStatic(
            "AddEvent",
            tracker,
            "upload-1",
            "UploadCreated",
            ProcessingStage.UPLOADED,
            ProcessingStatus.PENDING,
            "{\"correlationId\":\"upload-1\"}",
            null);

        var evt = Assert.Single(tracker.Events);
        Assert.Equal(ProcessingStage.UPLOADED, evt.Stage);
        Assert.Equal(ProcessingStatus.PENDING, evt.Status);
    }

    [Fact]
    public void ProgressFor_ReturnsExpectedMilestones()
    {
        Assert.Equal(10, InvokePrivateStatic<int>("ProgressFor", ProcessingStage.UPLOADED));
        Assert.Equal(35, InvokePrivateStatic<int>("ProgressFor", ProcessingStage.OCR_COMPLETED));
        Assert.Equal(60, InvokePrivateStatic<int>("ProgressFor", ProcessingStage.METADATA_COMPLETED));
        Assert.Equal(75, InvokePrivateStatic<int>("ProgressFor", ProcessingStage.QUALITY_SCORING_COMPLETED));
        Assert.Equal(85, InvokePrivateStatic<int>("ProgressFor", ProcessingStage.OPENALEX_COMPLETED));
        Assert.Equal(100, InvokePrivateStatic<int>("ProgressFor", ProcessingStage.COMPLETED));
    }

    [Theory]
    [InlineData(ProcessingStage.OCR_REQUESTED, ProcessingStatus.PROCESSING, "NougatMarkdownConversionStarted", "MarkdownStatus", "Processing")]
    [InlineData(ProcessingStage.OCR_COMPLETED, ProcessingStatus.COMPLETED, "NougatMarkdownConversionCompleted", "MarkdownStatus", "Completed")]
    [InlineData(ProcessingStage.FAILED, ProcessingStatus.FAILED, "NougatMarkdownConversionFailed", "MarkdownStatus", "Failed")]
    [InlineData(ProcessingStage.METADATA_COMPLETED, ProcessingStatus.COMPLETED, "GrobidMetadataExtractionCompleted", "MetadataExtractionStatus", "Completed")]
    [InlineData(ProcessingStage.METADATA_REQUESTED, ProcessingStatus.COMPLETED, "CrossrefEnrichmentCompleted", "CrossrefStatus", "Completed")]
    [InlineData(ProcessingStage.METADATA_REQUESTED, ProcessingStatus.COMPLETED, "CrossrefEnrichmentSkipped", "CrossrefStatus", "Skipped")]
    [InlineData(ProcessingStage.QUALITY_SCORING_COMPLETED, ProcessingStatus.COMPLETED, "MetadataQualityScoringCompleted", "MetadataQualityStatus", "Completed")]
    [InlineData(ProcessingStage.OPENALEX_COMPLETED, ProcessingStatus.COMPLETED, "OpenAlexSimilarityCheckCompleted", "OpenAlexStatus", "Completed")]
    public void LegacyStatusUpdatesFor_MapsWorkflowEventsToLegacyColumns(
        ProcessingStage stage,
        ProcessingStatus status,
        string eventType,
        string propertyName,
        string expectedValue)
    {
        var updates = InvokePrivateStatic<IReadOnlyDictionary<string, string>>(
            "LegacyStatusUpdatesFor",
            stage,
            status,
            eventType);

        Assert.Equal(expectedValue, updates[propertyName]);
    }

    [Fact]
    public void LegacyStatusUpdatesFor_FinalCompletionSetsCurrentStepCompleted()
    {
        var updates = InvokePrivateStatic<IReadOnlyDictionary<string, string>>(
            "LegacyStatusUpdatesFor",
            ProcessingStage.COMPLETED,
            ProcessingStatus.COMPLETED,
            "PaperProcessingCompleted");

        Assert.Equal("Completed", updates["CurrentStep"]);
    }

    private static void InvokePrivateStatic(string methodName, params object?[] parameters)
    {
        _ = InvokePrivateStatic<object?>(methodName, parameters);
    }

    private static T InvokePrivateStatic<T>(string methodName, params object?[] parameters)
    {
        var method = typeof(PaperProcessingTrackerService).GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        return (T)method!.Invoke(null, parameters)!;
    }
}
