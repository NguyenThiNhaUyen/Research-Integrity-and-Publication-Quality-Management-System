using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Configurations;
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
            PaperVersionId = 2,
            CorrelationId = "corr-1"
        };

        InvokePrivateStatic(
            "AddEvent",
            tracker,
            "PaperUploadedIntegrationEvent",
            ProcessingStage.UPLOADED,
            ProcessingStatus.PROCESSING,
            "{\"topic\":\"ripqms.paper-uploaded.v1\"}",
            null);

        var evt = Assert.Single(tracker.Events);
        Assert.True(Guid.TryParseExact(evt.EventId, "N", out _));
        Assert.Equal("corr-1", evt.CorrelationId);
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
            PaperVersionId = 2,
            CorrelationId = "upload-1"
        };

        InvokePrivateStatic(
            "AddEvent",
            tracker,
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
    public void AddEvent_GeneratesUniqueEventIdsAndKeepsWorkflowCorrelation()
    {
        var tracker = new PaperProcessingTracker
        {
            Id = 10,
            PaperId = 1,
            PaperVersionId = 2,
            CorrelationId = "corr-1"
        };

        InvokePrivateStatic(
            "AddEvent",
            tracker,
            "FirstEvent",
            ProcessingStage.UPLOADED,
            ProcessingStatus.PROCESSING,
            null,
            null);
        InvokePrivateStatic(
            "AddEvent",
            tracker,
            "SecondEvent",
            ProcessingStage.UPLOADED,
            ProcessingStatus.PROCESSING,
            null,
            null);

        Assert.Equal(2, tracker.Events.Count);
        Assert.All(tracker.Events, evt =>
        {
            Assert.True(Guid.TryParseExact(evt.EventId, "N", out _));
            Assert.Equal("corr-1", evt.CorrelationId);
            Assert.Equal("{}", evt.PayloadJson);
            Assert.NotEqual(evt.TrackerId.ToString(), evt.EventId);
            Assert.NotEqual(evt.PaperId.ToString(), evt.EventId);
            Assert.NotEqual(evt.PaperVersionId.ToString(), evt.EventId);
        });
        Assert.Equal(2, tracker.Events.Select(x => x.EventId).Distinct().Count());
    }

    [Fact]
    public void AddEvent_GeneratesTrackerCorrelationWhenMissing()
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
            "InternalEvent",
            ProcessingStage.UPLOADED,
            ProcessingStatus.PROCESSING,
            null,
            null);

        var evt = Assert.Single(tracker.Events);
        Assert.False(string.IsNullOrWhiteSpace(tracker.CorrelationId));
        Assert.Equal(tracker.CorrelationId, evt.CorrelationId);
    }

    [Fact]
    public void AddEvent_BlankEventTypeThrows()
    {
        var tracker = new PaperProcessingTracker
        {
            Id = 10,
            PaperId = 1,
            PaperVersionId = 2,
            CorrelationId = "corr-1"
        };

        var exception = Assert.Throws<TargetInvocationException>(() => InvokePrivateStatic(
            "AddEvent",
            tracker,
            " ",
            ProcessingStage.UPLOADED,
            ProcessingStatus.PROCESSING,
            null,
            null));

        Assert.IsType<ArgumentException>(exception.InnerException);
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

    [Fact]
    public void EfModel_DoesNotContainShadowBusinessPropertiesOnNormalEntities()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=model_validation;Username=model_validation;Password=model_validation")
            .Options;
        using var db = new ApplicationDbContext(options);

        var invalidShadowProperties = db.Model.GetEntityTypes()
            .Where(entityType => entityType.ClrType != typeof(Dictionary<string, object>))
            .SelectMany(entityType => entityType.GetProperties()
                .Where(property => property.IsShadowProperty() && !IsAllowedShadowProperty(entityType, property))
                .Select(property => $"{entityType.ClrType.Name}.{property.Name}"))
            .ToArray();

        Assert.Empty(invalidShadowProperties);
    }

    [Fact]
    public void EfModel_PaperProcessingEventIdentityIsRequiredAndUnique()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=model_validation;Username=model_validation;Password=model_validation")
            .Options;
        using var db = new ApplicationDbContext(options);

        var entityType = db.Model.FindEntityType(typeof(PaperProcessingEvent));
        Assert.NotNull(entityType);

        var eventId = entityType!.FindProperty(nameof(PaperProcessingEvent.EventId));
        var correlationId = entityType.FindProperty(nameof(PaperProcessingEvent.CorrelationId));
        var payloadJson = entityType.FindProperty(nameof(PaperProcessingEvent.PayloadJson));
        var index = entityType.GetIndexes()
            .SingleOrDefault(x => x.GetDatabaseName() == "IX_PaperProcessingEvents_EventId");

        Assert.NotNull(eventId);
        Assert.False(eventId!.IsNullable);
        Assert.NotNull(correlationId);
        Assert.False(correlationId!.IsNullable);
        Assert.NotNull(payloadJson);
        Assert.False(payloadJson!.IsNullable);
        Assert.Equal("'{}'::jsonb", payloadJson.GetDefaultValueSql());
        Assert.NotNull(index);
        Assert.True(index!.IsUnique);
    }

    private static bool IsAllowedShadowProperty(IEntityType entityType, IProperty property)
    {
        if (entityType.IsOwned())
        {
            return true;
        }

        return property.IsForeignKey()
            && property.Name.EndsWith("Id", StringComparison.Ordinal);
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
