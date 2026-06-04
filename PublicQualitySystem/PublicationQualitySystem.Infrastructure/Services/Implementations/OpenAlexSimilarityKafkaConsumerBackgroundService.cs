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

public sealed class OpenAlexSimilarityKafkaConsumerBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> options,
    ILogger<OpenAlexSimilarityKafkaConsumerBackgroundService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string Source = "OpenAlex";

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);

    private void ConsumeLoop(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "OpenAlex similarity Kafka consumer started. Topic={Topic}, GroupId={GroupId}, BootstrapServers={BootstrapServers}",
            options.Value.OpenAlexSimilarityRequestedTopic,
            options.Value.OpenAlexSimilarityConsumerGroupId,
            options.Value.BootstrapServers);

        using var consumer = new ConsumerBuilder<string, string>(BuildConsumerConfig(options.Value.OpenAlexSimilarityConsumerGroupId)).Build();
        consumer.Subscribe(options.Value.OpenAlexSimilarityRequestedTopic);

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
                    logger.LogError(ex, "OpenAlex similarity Kafka consume failed. Reason={Reason}", ex.Error.Reason);
                    continue;
                }

                if (result is null)
                {
                    continue;
                }

                var message = Deserialize(result.Message.Value);
                if (message is null)
                {
                    logger.LogWarning("OpenAlex similarity Kafka message skipped because payload is invalid. Topic={Topic}, Offset={Offset}", result.Topic, result.Offset.Value);
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
            logger.LogInformation("OpenAlex similarity Kafka consumer stopped.");
        }
    }

    private ConsumerConfig BuildConsumerConfig(string groupId) => new()
    {
        BootstrapServers = options.Value.BootstrapServers,
        GroupId = groupId,
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnableAutoCommit = false
    };

    private static OpenAlexSimilarityCheckRequestedIntegrationEvent? Deserialize(string payload)
    {
        try
        {
            return JsonSerializer.Deserialize<OpenAlexSimilarityCheckRequestedIntegrationEvent>(payload, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task ProcessMessageAsync(OpenAlexSimilarityCheckRequestedIntegrationEvent message, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var openAlex = scope.ServiceProvider.GetRequiredService<IOpenAlexService>();
        var processingTracker = scope.ServiceProvider.GetRequiredService<IPaperProcessingTrackerService>();

        var metadata = await db.PaperMetadata
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == message.PaperMetadataId && x.PaperId == message.PaperId, cancellationToken);

        if (metadata is null)
        {
            logger.LogWarning(
                "OpenAlex similarity check skipped because PaperMetadata was not found. PaperId={PaperId}, PaperMetadataId={PaperMetadataId}",
                message.PaperId,
                message.PaperMetadataId);
            return;
        }

        var check = await db.PaperSimilarityChecks
            .FirstOrDefaultAsync(x => x.PaperMetadataId == metadata.Id && x.Source == Source, cancellationToken);

        if (check?.Status is SimilarityCheckStatus.COMPLETED or SimilarityCheckStatus.SKIPPED)
        {
            logger.LogInformation(
                "OpenAlex similarity duplicate ignored. PaperId={PaperId}, PaperMetadataId={PaperMetadataId}, Status={Status}",
                metadata.PaperId,
                metadata.Id,
                check.Status);
            return;
        }

        if (check is null)
        {
            check = new PaperSimilarityCheck
            {
                PaperId = metadata.PaperId,
                PaperMetadataId = metadata.Id,
                Source = Source,
                Status = SimilarityCheckStatus.PENDING,
                RiskLevel = SimilarityRiskLevel.LOW
            };
            db.PaperSimilarityChecks.Add(check);
            await db.SaveChangesAsync(cancellationToken);
        }

        try
        {
            await processingTracker.MarkStepStartedAsync(message.PaperVersionId, PaperProcessingStep.OpenAlexSimilarityCheck, cancellationToken);
            var result = await openAlex.CheckSimilarityAsync(metadata, cancellationToken);
            check.Status = SimilarityCheckStatus.COMPLETED;
            check.CheckedAt = DateTime.UtcNow;
            check.ErrorMessage = null;

            if (result is null)
            {
                check.OverallScore = 0;
                check.RiskLevel = SimilarityRiskLevel.LOW;
                check.RawJson = "{}";
            }
            else
            {
                check.MatchedOpenAlexId = result.MatchedOpenAlexId;
                check.MatchedDoi = result.MatchedDoi;
                check.MatchedTitle = result.MatchedTitle;
                check.TitleSimilarity = result.TitleSimilarity;
                check.AuthorSimilarity = result.AuthorSimilarity;
                check.AbstractSimilarity = result.AbstractSimilarity;
                check.ReferenceSimilarity = result.ReferenceSimilarity;
                check.OverallScore = result.OverallScore;
                check.RiskLevel = result.RiskLevel;
                check.RawJson = result.RawJson;
            }

            await db.SaveChangesAsync(cancellationToken);
            await processingTracker.MarkStepCompletedAsync(
                message.PaperVersionId,
                PaperProcessingStep.OpenAlexSimilarityCheck,
                $"{{\"overallScore\":{check.OverallScore ?? 0},\"riskLevel\":\"{check.RiskLevel}\"}}",
                cancellationToken);
            logger.LogInformation(
                "OpenAlex similarity check completed. PaperId={PaperId}, PaperMetadataId={PaperMetadataId}, OverallScore={OverallScore}, RiskLevel={RiskLevel}",
                metadata.PaperId,
                metadata.Id,
                check.OverallScore,
                check.RiskLevel);
        }
        catch (Exception ex)
        {
            check.Status = SimilarityCheckStatus.FAILED;
            check.ErrorMessage = ex.Message;
            check.CheckedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(CancellationToken.None);
            await processingTracker.MarkStepFailedAsync(
                message.PaperVersionId,
                PaperProcessingStep.OpenAlexSimilarityCheck,
                "OpenAlexSimilarityFailed",
                ex.Message,
                cancellationToken: CancellationToken.None);

            logger.LogWarning(
                ex,
                "OpenAlex similarity check failed. PaperId={PaperId}, PaperMetadataId={PaperMetadataId}",
                metadata.PaperId,
                metadata.Id);
        }
    }
}
