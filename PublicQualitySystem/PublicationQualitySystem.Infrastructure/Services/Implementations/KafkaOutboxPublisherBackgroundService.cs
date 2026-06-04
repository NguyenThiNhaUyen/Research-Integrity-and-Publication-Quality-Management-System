using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Text.Json;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Configurations;
using PublicationQualitySystem.Infrastructure.Options;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public sealed class KafkaOutboxPublisherBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> options,
    ILogger<KafkaOutboxPublisherBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Kafka outbox publisher started. BootstrapServers={BootstrapServers}, IntervalSeconds={IntervalSeconds}",
            options.Value.BootstrapServers,
            options.Value.OutboxPublisherIntervalSeconds);

        using var producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        }).Build();

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.OutboxPublisherIntervalSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            await PublishBatchAsync(producer, stoppingToken);

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        logger.LogInformation("Kafka outbox publisher stopped.");
    }

    private async Task PublishBatchAsync(IProducer<string, string> producer, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var messages = await db.OutboxMessages
            .Where(x => x.Status != OutboxMessageStatus.Published)
            .OrderBy(x => x.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var outbox in messages)
        {
            try
            {
                outbox.Status = OutboxMessageStatus.Processing;
                outbox.LastError = null;
                await db.SaveChangesAsync(cancellationToken);

                var result = await producer.ProduceAsync(
                    outbox.Topic,
                    new Message<string, string>
                    {
                        Key = outbox.Key,
                        Value = outbox.Payload,
                        Headers = new Headers
                        {
                            new Header("event-type", System.Text.Encoding.UTF8.GetBytes(outbox.Type))
                        }
                    },
                    cancellationToken);

                outbox.Status = OutboxMessageStatus.Published;
                outbox.PublishedAt = DateTime.UtcNow;
                outbox.LastError = null;
                await db.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Outbox message published to Kafka. OutboxMessageId={OutboxMessageId}, Topic={Topic}, Partition={Partition}, Offset={Offset}",
                    outbox.Id,
                    outbox.Topic,
                    result.Partition.Value,
                    result.Offset.Value);

                await RecordWorkflowPublishEventAsync(scope.ServiceProvider, outbox, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                outbox.Status = OutboxMessageStatus.Failed;
                outbox.RetryCount++;
                outbox.LastError = ex.Message;
                await db.SaveChangesAsync(CancellationToken.None);

                logger.LogError(
                    ex,
                    "Outbox message publish failed. OutboxMessageId={OutboxMessageId}, Topic={Topic}, RetryCount={RetryCount}",
                    outbox.Id,
                    outbox.Topic,
                    outbox.RetryCount);
            }
        }
    }

    private async Task RecordWorkflowPublishEventAsync(
        IServiceProvider serviceProvider,
        OutboxMessage outbox,
        CancellationToken cancellationToken)
    {
        if (!TryGetPaperVersionId(outbox.Payload, out var paperVersionId))
        {
            return;
        }

        var tracker = serviceProvider.GetService<IPaperProcessingTrackerService>();
        if (tracker is null)
        {
            return;
        }

        try
        {
            await tracker.RecordEventPublishedAsync(
                paperVersionId,
                ResolveStage(outbox.Type),
                TryGetString(outbox.Payload, "EventId") ?? outbox.Id.ToString(),
                $"{outbox.Type}PublishedToKafka",
                outbox.Payload,
                cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                ex,
                "Kafka workflow publish tracking failed. OutboxMessageId={OutboxMessageId}, Type={Type}",
                outbox.Id,
                outbox.Type);
        }
    }

    private static ProcessingStage ResolveStage(string eventType)
    {
        if (eventType.Contains("OpenAlexSimilarityCheckRequested", StringComparison.OrdinalIgnoreCase))
        {
            return ProcessingStage.OPENALEX_REQUESTED;
        }

        if (eventType.Contains("OpenAlexSimilarityCheckSkipped", StringComparison.OrdinalIgnoreCase))
        {
            return ProcessingStage.OPENALEX_COMPLETED;
        }

        if (eventType.Contains("MetadataQualityScored", StringComparison.OrdinalIgnoreCase))
        {
            return ProcessingStage.QUALITY_SCORING_COMPLETED;
        }

        return ProcessingStage.UPLOADED;
    }

    private static bool TryGetPaperVersionId(string payload, out long paperVersionId)
    {
        paperVersionId = 0;
        var rawValue = TryGetString(payload, "PaperVersionId");
        return long.TryParse(rawValue, out paperVersionId);
    }

    private static string? TryGetString(string payload, string propertyName)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            foreach (var property in doc.RootElement.EnumerateObject())
            {
                if (!string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Number => property.Value.GetRawText(),
                    _ => null
                };
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }
}
