using System.Diagnostics;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PublicationQualitySystem.Application.DTOs.Crossref;
using PublicationQualitySystem.Application.DTOs.Grobid;
using PublicationQualitySystem.Application.DTOs.IntegrationEvents;
using PublicationQualitySystem.Application.DTOs.Metadata;
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
        var metadataNormalizer = scope.ServiceProvider.GetRequiredService<IMetadataNormalizerService>();
        var qualityScoring = scope.ServiceProvider.GetRequiredService<IMetadataQualityScoringService>();
        var doiCheck = scope.ServiceProvider.GetRequiredService<IPaperDoiCheckService>();
        var auditLog = scope.ServiceProvider.GetRequiredService<IAuditLogService>();
        var processingTracker = scope.ServiceProvider.GetRequiredService<IPaperProcessingTrackerService>();

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
            await processingTracker.RecordStepStartedAsync(
                version.Id,
                ProcessingStage.METADATA_REQUESTED,
                "GrobidMetadataExtractionStarted",
                cancellationToken: cancellationToken);

            await auditLog.StartStepAsync(
                ProcessingStep.GROBID_METADATA_EXTRACTION,
                "Extract metadata with GROBID",
                paperId: version.PaperId,
                paperVersionId: version.Id,
                correlationId: message.CorrelationId,
                message: "GROBID metadata extraction started.",
                metadata: new { version.PdfS3Key, version.OriginalFileName },
                cancellationToken: cancellationToken);

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

            var rawGrobidMetadata = JsonSerializer.Deserialize<GrobidMetadataResponse>(
                JsonSerializer.Serialize(extracted, JsonOptions),
                JsonOptions) ?? extracted;
            rawGrobidMetadata.RawGrobidXml = string.Empty;
            CrossrefMetadataResponse? crossrefMetadata = null;
            if (!string.IsNullOrWhiteSpace(extracted.Doi))
            {
                await processingTracker.RecordStepStartedAsync(
                    version.Id,
                    ProcessingStage.METADATA_REQUESTED,
                    "CrossrefEnrichmentStarted",
                    cancellationToken: cancellationToken);
                crossrefMetadata = await crossref.GetWorkByDoiAsync(extracted.Doi, cancellationToken);
                extracted = ScholarlyMetadataMerger.Merge(extracted, crossrefMetadata);
                if (crossrefMetadata is null)
                {
                    await processingTracker.RecordStepSkippedAsync(
                        version.Id,
                        ProcessingStage.METADATA_REQUESTED,
                        "CrossrefEnrichmentSkipped",
                        "Crossref returned no enrichment data.",
                        cancellationToken);
                }
                else
                {
                    await processingTracker.RecordStepCompletedAsync(
                        version.Id,
                        ProcessingStage.METADATA_REQUESTED,
                        "CrossrefEnrichmentCompleted",
                        cancellationToken: cancellationToken);
                }
            }
            else
            {
                logger.LogInformation(
                    "Crossref lookup skipped because DOI was not found. PaperId={PaperId}, PaperVersionId={PaperVersionId}",
                    version.PaperId,
                    version.Id);
                await processingTracker.RecordStepSkippedAsync(
                    version.Id,
                    ProcessingStage.METADATA_REQUESTED,
                    "CrossrefEnrichmentSkipped",
                    "DOI was not found.",
                    cancellationToken);
            }

            var normalization = metadataNormalizer.Normalize(extracted, crossrefMetadata);
            var normalized = normalization.Metadata;
            metadata.Title = normalized.Title;
            metadata.Abstract = normalized.Abstract;
            metadata.Doi = normalized.Doi;
            metadata.ArxivId = normalized.ArxivId;
            metadata.Journal = normalized.Journal;
            metadata.Publisher = normalized.Publisher;
            metadata.Venue = normalized.Venue;
            metadata.ConferenceName = normalized.ConferenceName;
            metadata.PublicationYear = normalized.PublicationYear;
            metadata.Volume = normalized.Volume;
            metadata.Issue = normalized.Issue;
            metadata.Pages = normalized.Pages;
            metadata.CorrespondingAuthor = normalized.CorrespondingAuthor;
            metadata.ReceivedDate = normalized.ReceivedDate;
            metadata.RevisedDate = normalized.RevisedDate;
            metadata.AcceptedDate = normalized.AcceptedDate;
            metadata.PublishedDate = normalized.PublishedDate;
            metadata.OpenAccessLicense = normalized.OpenAccessLicense;
            metadata.MetadataSource = normalized.MetadataSource;
            metadata.DoiSource = normalized.DoiSource;
            metadata.JournalSource = normalized.JournalSource;
            metadata.KeywordsJson = JsonSerializer.Serialize(normalized.Keywords, JsonOptions);
            metadata.FundingOrganizationsJson = JsonSerializer.Serialize(normalized.FundingOrganizations, JsonOptions);
            metadata.AuthorsJson = JsonSerializer.Serialize(normalized.Authors, JsonOptions);
            metadata.ReferencesJson = JsonSerializer.Serialize(normalized.References, JsonOptions);
            metadata.RawMetadataJson = JsonSerializer.Serialize(new RawMetadataSnapshot
            {
                Grobid = rawGrobidMetadata,
                Crossref = crossrefMetadata,
                MetadataSource = extracted.MetadataSource
            }, JsonOptions);
            metadata.NormalizedMetadataJson = JsonSerializer.Serialize(new NormalizedMetadataSnapshot
            {
                Title = normalized.Title,
                Doi = normalized.Doi,
                Journal = normalized.Journal,
                Publisher = normalized.Publisher,
                Venue = normalized.Venue,
                PublicationYear = normalized.PublicationYear,
                Volume = normalized.Volume,
                Issue = normalized.Issue,
                Pages = normalized.Pages,
                ReceivedDate = normalized.ReceivedDate,
                RevisedDate = normalized.RevisedDate,
                AcceptedDate = normalized.AcceptedDate,
                PublishedDate = normalized.PublishedDate,
                OpenAccessLicense = normalized.OpenAccessLicense,
                Keywords = normalized.Keywords,
                References = normalized.References
            }, JsonOptions);
            metadata.MetadataCleanlinessScore = normalization.MainMetadataCleanlinessScore;
            metadata.ReferenceCleanlinessScore = normalization.ReferenceCleanlinessScore;
            metadata.DirtyFieldCount = normalization.DirtyFieldCount;
            metadata.MetadataIssueCodesJson = JsonSerializer.Serialize(normalization.IssueCodes, JsonOptions);
            metadata.MetadataWarningsJson = JsonSerializer.Serialize(normalization.WarningMessages, JsonOptions);
            metadata.RawGrobidXml = normalized.RawGrobidXml;
            metadata.ExtractionStatus = MetadataExtractionStatus.Completed;
            metadata.ExtractionError = null;
            metadata.ExtractedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken);
            await processingTracker.RecordStepCompletedAsync(
                version.Id,
                ProcessingStage.METADATA_COMPLETED,
                "GrobidMetadataExtractionCompleted",
                cancellationToken: cancellationToken);

            await auditLog.CompleteStepAsync(
                ProcessingStep.GROBID_METADATA_EXTRACTION,
                "Extract metadata with GROBID",
                paperId: version.PaperId,
                paperVersionId: version.Id,
                correlationId: message.CorrelationId,
                message: "GROBID metadata extraction completed.",
                metadata: new
                {
                    hasTitle = !string.IsNullOrWhiteSpace(metadata.Title),
                    hasDoi = !string.IsNullOrWhiteSpace(metadata.Doi),
                    authorCount = extracted.Authors.Count,
                    referenceCount = extracted.References.Count
                },
                cancellationToken: cancellationToken);

            await doiCheck.RunAsync(
                version.PaperId,
                metadata.Id,
                version.Id,
                message.CorrelationId,
                cancellationToken);

            await auditLog.StartStepAsync(
                ProcessingStep.METADATA_QUALITY_SCORING,
                "Calculate metadata quality score",
                paperId: version.PaperId,
                paperVersionId: version.Id,
                correlationId: message.CorrelationId,
                message: "Metadata quality scoring started.",
                cancellationToken: cancellationToken);
            await processingTracker.RecordStepStartedAsync(
                version.Id,
                ProcessingStage.QUALITY_SCORING_REQUESTED,
                "MetadataQualityScoringStarted",
                cancellationToken: cancellationToken);

            var qualityScore = qualityScoring.Calculate(metadata);
            MetadataQualityScoreMapper.Apply(metadata, qualityScore, DateTime.UtcNow);
            var metadataQualityScoredEvent = new MetadataQualityScoredIntegrationEvent
            {
                PaperId = version.PaperId,
                PaperVersionId = version.Id,
                PaperMetadataId = metadata.Id,
                TotalScore = qualityScore.TotalScore,
                CoreScore = qualityScore.CoreScore,
                Grade = qualityScore.Grade,
                CanProceed = qualityScore.CanProceed,
                CorrelationId = message.CorrelationId
            };
            AddOutbox(db, options.Value.MetadataQualityScoredTopic, version.PaperId.ToString(), metadataQualityScoredEvent);
            await db.SaveChangesAsync(cancellationToken);
            await processingTracker.RecordStepCompletedAsync(
                version.Id,
                ProcessingStage.QUALITY_SCORING_COMPLETED,
                "MetadataQualityScoringCompleted",
                cancellationToken: cancellationToken);
            await processingTracker.RecordEventPublishedAsync(
                version.Id,
                ProcessingStage.QUALITY_SCORING_COMPLETED,
                metadataQualityScoredEvent.EventId,
                nameof(MetadataQualityScoredIntegrationEvent),
                JsonSerializer.Serialize(metadataQualityScoredEvent, JsonOptions),
                cancellationToken);

            await auditLog.CompleteStepAsync(
                ProcessingStep.METADATA_QUALITY_SCORING,
                "Calculate metadata quality score",
                paperId: version.PaperId,
                paperVersionId: version.Id,
                correlationId: message.CorrelationId,
                message: "Metadata quality scoring completed.",
                metadata: new
                {
                    qualityScore.TotalScore,
                    qualityScore.Grade,
                    qualityScore.CanProceed,
                    qualityScore.MissingFields
                },
                cancellationToken: cancellationToken);

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
                    "OpenAlex similarity check will be skipped by metadata quality gate. PaperId={PaperId}, PaperVersionId={PaperVersionId}, TotalScore={TotalScore}, Grade={Grade}",
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
            await processingTracker.RecordStepFailedAsync(
                version.Id,
                ProcessingStage.FAILED,
                "GrobidMetadataExtractionFailed",
                ex.Message,
                cancellationToken: CancellationToken.None);
            await auditLog.FailStepAsync(
                ProcessingStep.GROBID_METADATA_EXTRACTION,
                "Extract metadata with GROBID",
                ex.Message,
                paperId: version.PaperId,
                paperVersionId: version.Id,
                correlationId: message.CorrelationId,
                message: "GROBID metadata extraction failed.",
                metadata: new { version.PdfS3Key, version.OriginalFileName },
                cancellationToken: CancellationToken.None);
        }
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
