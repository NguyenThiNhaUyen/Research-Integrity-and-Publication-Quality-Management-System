using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PublicationQualitySystem.Application.DTOs.IntegrationEvents;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Configurations;
using PublicationQualitySystem.Infrastructure.Options;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public sealed class OpenAlexGateKafkaConsumerBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> options,
    ILogger<OpenAlexGateKafkaConsumerBackgroundService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string Source = "OpenAlex";
    private const string GateFailedReason = "Metadata quality gate failed";

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);

    private void ConsumeLoop(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "OpenAlex gate Kafka consumer started. Topic={Topic}, GroupId={GroupId}, BootstrapServers={BootstrapServers}",
            options.Value.MetadataQualityScoredTopic,
            options.Value.OpenAlexGateConsumerGroupId,
            options.Value.BootstrapServers);

        using var consumer = new ConsumerBuilder<string, string>(BuildConsumerConfig(options.Value.OpenAlexGateConsumerGroupId)).Build();
        consumer.Subscribe(options.Value.MetadataQualityScoredTopic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result;
                try
                {
                    result = consumer.Consume(TimeSpan.FromMilliseconds(options.Value.ConsumerPollTimeoutMs));
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "OpenAlex gate Kafka consume failed. Reason={Reason}", ex.Error.Reason);
                    continue;
                }

                if (result is null)
                {
                    continue;
                }

                var message = Deserialize(result.Message.Value);
                if (message is null)
                {
                    logger.LogWarning("OpenAlex gate Kafka message skipped because payload is invalid. Topic={Topic}, Offset={Offset}", result.Topic, result.Offset.Value);
                    consumer.Commit(result);
                    continue;
                }

                ProcessMessageAsync(message, stoppingToken).GetAwaiter().GetResult();
                consumer.Commit(result);
            }
        }
        finally
        {
            consumer.Close();
            logger.LogInformation("OpenAlex gate Kafka consumer stopped.");
        }
    }

    private ConsumerConfig BuildConsumerConfig(string groupId) => new()
    {
        BootstrapServers = options.Value.BootstrapServers,
        GroupId = groupId,
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnableAutoCommit = false
    };

    private static MetadataQualityScoredIntegrationEvent? Deserialize(string payload)
    {
        try
        {
            return JsonSerializer.Deserialize<MetadataQualityScoredIntegrationEvent>(payload, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task ProcessMessageAsync(MetadataQualityScoredIntegrationEvent message, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var processingTracker = scope.ServiceProvider.GetRequiredService<IPaperProcessingTrackerService>();

        var metadata = await db.PaperMetadata
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == message.PaperMetadataId && x.PaperId == message.PaperId, cancellationToken);

        if (metadata is null)
        {
            logger.LogWarning(
                "OpenAlex gate skipped because PaperMetadata was not found. PaperId={PaperId}, PaperMetadataId={PaperMetadataId}",
                message.PaperId,
                message.PaperMetadataId);
            return;
        }

        var existing = await db.PaperSimilarityChecks
            .FirstOrDefaultAsync(x => x.PaperMetadataId == metadata.Id && x.Source == Source, cancellationToken);
        if (existing is not null)
        {
            logger.LogInformation(
                "OpenAlex gate duplicate ignored. PaperId={PaperId}, PaperMetadataId={PaperMetadataId}, Status={Status}",
                metadata.PaperId,
                metadata.Id,
                existing.Status);
            return;
        }

        if (metadata.MetadataQualityCanProceed == true)
        {
            db.PaperSimilarityChecks.Add(new PaperSimilarityCheck
            {
                PaperId = metadata.PaperId,
                PaperMetadataId = metadata.Id,
                Source = Source,
                Status = SimilarityCheckStatus.PENDING,
                RiskLevel = SimilarityRiskLevel.LOW
            });

            var requested = new OpenAlexSimilarityCheckRequestedIntegrationEvent
            {
                PaperId = message.PaperId,
                PaperVersionId = message.PaperVersionId,
                PaperMetadataId = message.PaperMetadataId,
                CorrelationId = message.CorrelationId
            };
            AddOutbox(db, options.Value.OpenAlexSimilarityRequestedTopic, message.PaperId.ToString(), requested);
            await db.SaveChangesAsync(cancellationToken);
            await processingTracker.RecordEventPublishedAsync(
                message.PaperVersionId,
                ProcessingStage.OPENALEX_REQUESTED,
                requested.EventId,
                nameof(OpenAlexSimilarityCheckRequestedIntegrationEvent),
                JsonSerializer.Serialize(requested, JsonOptions),
                cancellationToken);

            logger.LogInformation(
                "OpenAlex similarity check requested. PaperId={PaperId}, PaperMetadataId={PaperMetadataId}, TotalScore={TotalScore}, CoreScore={CoreScore}, Grade={Grade}",
                metadata.PaperId,
                metadata.Id,
                metadata.MetadataQualityTotalScore,
                metadata.MetadataQualityCoreScore,
                metadata.MetadataQualityGrade);
            return;
        }

        db.PaperSimilarityChecks.Add(new PaperSimilarityCheck
        {
            PaperId = metadata.PaperId,
            PaperMetadataId = metadata.Id,
            Source = Source,
            Status = SimilarityCheckStatus.SKIPPED,
            RiskLevel = SimilarityRiskLevel.SKIPPED,
            OverallScore = 0,
            SkipReason = GateFailedReason,
            CheckedAt = DateTime.UtcNow
        });

        var skipped = new OpenAlexSimilarityCheckSkippedIntegrationEvent
        {
            PaperId = message.PaperId,
            PaperVersionId = message.PaperVersionId,
            PaperMetadataId = message.PaperMetadataId,
            TotalScore = metadata.MetadataQualityTotalScore ?? message.TotalScore,
            CoreScore = metadata.MetadataQualityCoreScore ?? message.CoreScore,
            Grade = metadata.MetadataQualityGrade ?? message.Grade,
            MissingFieldsJson = metadata.MetadataQualityMissingFieldsJson,
            WarningsJson = metadata.MetadataQualityWarningsJson,
            CorrelationId = message.CorrelationId
        };
        AddOutbox(db, options.Value.OpenAlexSimilaritySkippedTopic, message.PaperId.ToString(), skipped);
        await db.SaveChangesAsync(cancellationToken);
        await processingTracker.RecordStepSkippedAsync(
            message.PaperVersionId,
            ProcessingStage.OPENALEX_COMPLETED,
            "OpenAlexSimilarityCheckSkipped",
            GateFailedReason,
            cancellationToken);
        await processingTracker.RecordEventPublishedAsync(
            message.PaperVersionId,
            ProcessingStage.OPENALEX_COMPLETED,
            skipped.EventId,
            nameof(OpenAlexSimilarityCheckSkippedIntegrationEvent),
            JsonSerializer.Serialize(skipped, JsonOptions),
            cancellationToken);
        await processingTracker.RecordStepCompletedAsync(
            message.PaperVersionId,
            ProcessingStage.COMPLETED,
            "PaperProcessingCompleted",
            "{\"reason\":\"OpenAlex similarity check skipped by metadata quality gate.\"}",
            cancellationToken);

        logger.LogWarning(
            "OpenAlex similarity check skipped because metadata quality gate failed. PaperId={PaperId}, PaperMetadataId={PaperMetadataId}, TotalScore={TotalScore}, CoreScore={CoreScore}, Grade={Grade}",
            metadata.PaperId,
            metadata.Id,
            metadata.MetadataQualityTotalScore,
            metadata.MetadataQualityCoreScore,
            metadata.MetadataQualityGrade);
    }

    private static void AddOutbox<TEvent>(ApplicationDbContext db, string topic, string key, TEvent payload)
    {
        db.OutboxMessages.Add(new OutboxMessage
        {
            Topic = topic,
            Key = key,
            Type = typeof(TEvent).Name,
            Payload = JsonSerializer.Serialize(payload, JsonOptions),
            Status = OutboxMessageStatus.Pending
        });
    }
}
