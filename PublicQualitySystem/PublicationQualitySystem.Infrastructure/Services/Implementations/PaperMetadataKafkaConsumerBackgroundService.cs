using System.Diagnostics;
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

public sealed class PaperMetadataKafkaConsumerBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> options,
    ILogger<PaperMetadataKafkaConsumerBackgroundService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
    }

    private void ConsumeLoop(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Paper metadata Kafka consumer started. Topic={Topic}, GroupId={GroupId}, BootstrapServers={BootstrapServers}",
            options.Value.PaperUploadedTopic,
            options.Value.MetadataConsumerGroupId,
            options.Value.BootstrapServers);

        using var consumer = new ConsumerBuilder<string, string>(BuildConsumerConfig(options.Value.MetadataConsumerGroupId)).Build();
        consumer.Subscribe(options.Value.PaperUploadedTopic);

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
                    logger.LogError(ex, "Paper metadata Kafka consume failed. Reason={Reason}", ex.Error.Reason);
                    continue;
                }

                if (result is null)
                {
                    continue;
                }

                var message = Deserialize(result.Message.Value);
                if (message is null)
                {
                    logger.LogWarning(
                        "Paper metadata Kafka message skipped because payload is invalid. Topic={Topic}, Partition={Partition}, Offset={Offset}",
                        result.Topic,
                        result.Partition.Value,
                        result.Offset.Value);
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
            logger.LogInformation("Paper metadata Kafka consumer stopped.");
        }
    }

    private ConsumerConfig BuildConsumerConfig(string groupId) => new()
    {
        BootstrapServers = options.Value.BootstrapServers,
        GroupId = groupId,
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnableAutoCommit = false
    };

    private static PaperUploadedIntegrationEvent? Deserialize(string payload)
    {
        try
        {
            return JsonSerializer.Deserialize<PaperUploadedIntegrationEvent>(payload, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task ProcessMessageAsync(PaperUploadedIntegrationEvent message, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation(
            "Metadata extraction started. PaperId={PaperId}, PaperVersionId={PaperVersionId}",
            message.PaperId,
            message.PaperVersionId);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
        var grobid = scope.ServiceProvider.GetRequiredService<IGrobidService>();
        var crossref = scope.ServiceProvider.GetRequiredService<ICrossrefService>();
        var qualityScoring = scope.ServiceProvider.GetRequiredService<IMetadataQualityScoringService>();

        var version = await db.PaperVersions
            .Include(x => x.Paper)
            .ThenInclude(x => x.Metadata)
            .FirstOrDefaultAsync(x => x.Id == message.PaperVersionId && x.PaperId == message.PaperId, cancellationToken);

        if (version is null)
        {
            logger.LogWarning(
                "Metadata extraction skipped because PaperVersion was not found. PaperId={PaperId}, PaperVersionId={PaperVersionId}",
                message.PaperId,
                message.PaperVersionId);
            return;
        }

        var metadata = version.Paper.Metadata;
        if (metadata?.ExtractionStatus == MetadataExtractionStatus.Completed)
        {
            logger.LogInformation(
                "Metadata extraction skipped because metadata is already completed. PaperId={PaperId}, PaperVersionId={PaperVersionId}",
                message.PaperId,
                message.PaperVersionId);
            return;
        }

        if (metadata is null)
        {
            metadata = new PaperMetadata
            {
                PaperId = version.PaperId,
                ExtractionStatus = MetadataExtractionStatus.Pending
            };
            db.PaperMetadata.Add(metadata);
            await db.SaveChangesAsync(cancellationToken);
        }

        try
        {
            metadata.ExtractionStatus = MetadataExtractionStatus.Processing;
            metadata.ExtractionError = null;
            await db.SaveChangesAsync(cancellationToken);

            await using var pdfStream = await storage.DownloadAsync(version.PdfS3Key, cancellationToken);
            var extracted = await grobid.ExtractMetadataAsync(
                pdfStream,
                version.OriginalFileName,
                cancellationToken);
            logger.LogInformation(
                "DOI found. PaperId={PaperId}, PaperVersionId={PaperVersionId}, Doi={Doi}, DoiSource={DoiSource}",
                version.PaperId,
                version.Id,
                extracted.Doi,
                extracted.DoiSource);
            logger.LogInformation(
                "Journal found. PaperId={PaperId}, PaperVersionId={PaperVersionId}, Journal={Journal}, JournalSource={JournalSource}",
                version.PaperId,
                version.Id,
                extracted.Journal,
                extracted.JournalSource);

            if (!string.IsNullOrWhiteSpace(extracted.Doi))
            {
                var crossrefMetadata = await crossref.GetWorkByDoiAsync(extracted.Doi, cancellationToken);
                extracted = ScholarlyMetadataMerger.Merge(extracted, crossrefMetadata);
            }
            else
            {
                logger.LogInformation(
                    "Crossref lookup skipped because DOI was not found. PaperId={PaperId}, PaperVersionId={PaperVersionId}",
                    version.PaperId,
                    version.Id);
            }

            metadata.Title = extracted.Title;
            metadata.Abstract = extracted.Abstract;
            metadata.Doi = extracted.Doi;
            metadata.ArxivId = extracted.ArxivId;
            metadata.Journal = extracted.Journal;
            metadata.Publisher = extracted.Publisher;
            metadata.Venue = extracted.Venue;
            metadata.ConferenceName = extracted.ConferenceName;
            metadata.PublicationYear = extracted.PublicationYear;
            metadata.Volume = extracted.Volume;
            metadata.Issue = extracted.Issue;
            metadata.Pages = extracted.Pages;
            metadata.CorrespondingAuthor = extracted.CorrespondingAuthor;
            metadata.MetadataSource = extracted.MetadataSource;
            metadata.DoiSource = extracted.DoiSource;
            metadata.JournalSource = extracted.JournalSource;
            metadata.KeywordsJson = JsonSerializer.Serialize(extracted.Keywords, JsonOptions);
            metadata.AuthorsJson = JsonSerializer.Serialize(extracted.Authors, JsonOptions);
            metadata.ReferencesJson = JsonSerializer.Serialize(extracted.References, JsonOptions);
            metadata.RawGrobidXml = extracted.RawGrobidXml;
            metadata.ExtractionStatus = MetadataExtractionStatus.Completed;
            metadata.ExtractionError = null;
            metadata.ExtractedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken);

            var qualityScore = qualityScoring.Calculate(metadata);
            logger.LogInformation(
                "Metadata quality score calculated. PaperId={PaperId}, PaperVersionId={PaperVersionId}, TotalScore={TotalScore}, Grade={Grade}, CanProceed={CanProceed}, MissingFields={MissingFields}, Warnings={Warnings}",
                version.PaperId,
                version.Id,
                qualityScore.TotalScore,
                qualityScore.Grade,
                qualityScore.CanProceed,
                string.Join(", ", qualityScore.MissingFields),
                string.Join(" | ", qualityScore.Warnings));

            if (!qualityScore.CanProceed)
            {
                logger.LogWarning(
                    "Integrity Screening blocked by metadata quality gate. PaperId={PaperId}, PaperVersionId={PaperVersionId}, TotalScore={TotalScore}, Grade={Grade}",
                    version.PaperId,
                    version.Id,
                    qualityScore.TotalScore,
                    qualityScore.Grade);
            }

            logger.LogInformation(
                "Metadata saved. PaperId={PaperId}, PaperVersionId={PaperVersionId}, MetadataSource={MetadataSource}, DoiSource={DoiSource}, JournalSource={JournalSource}",
                version.PaperId,
                version.Id,
                metadata.MetadataSource,
                metadata.DoiSource,
                metadata.JournalSource);

            logger.LogInformation(
                "GROBID metadata parsed. PaperId={PaperId}, PaperVersionId={PaperVersionId}, Title={Title}, AuthorCount={AuthorCount}, ReferenceCount={ReferenceCount}, HasDoi={HasDoi}, ArxivId={ArxivId}",
                version.PaperId,
                version.Id,
                extracted.Title,
                extracted.Authors.Count,
                extracted.References.Count,
                !string.IsNullOrWhiteSpace(extracted.Doi),
                extracted.ArxivId);

            logger.LogInformation(
                "Metadata extraction completed. PaperId={PaperId}, PaperVersionId={PaperVersionId}, Status={Status}, ElapsedMs={ElapsedMs}",
                version.PaperId,
                version.Id,
                metadata.ExtractionStatus,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Metadata extraction failed. PaperId={PaperId}, PaperVersionId={PaperVersionId}, PdfS3Key={PdfS3Key}, ElapsedMs={ElapsedMs}",
                version.PaperId,
                version.Id,
                version.PdfS3Key,
                stopwatch.ElapsedMilliseconds);

            metadata.ExtractionStatus = MetadataExtractionStatus.Failed;
            metadata.ExtractionError = ex.Message;
            await db.SaveChangesAsync(CancellationToken.None);
        }
    }
}
