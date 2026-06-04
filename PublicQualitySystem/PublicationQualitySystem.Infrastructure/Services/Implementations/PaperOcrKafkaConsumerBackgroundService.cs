using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PublicationQualitySystem.Application.DTOs.IntegrationEvents;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Configurations;
using PublicationQualitySystem.Infrastructure.Options;
using PublicationQualitySystem.Shared.Exceptions;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public sealed class PaperOcrKafkaConsumerBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> options,
    ILogger<PaperOcrKafkaConsumerBackgroundService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
    }

    private void ConsumeLoop(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Paper OCR Kafka consumer started. Topic={Topic}, GroupId={GroupId}, BootstrapServers={BootstrapServers}",
            options.Value.PaperUploadedTopic,
            options.Value.OcrConsumerGroupId,
            options.Value.BootstrapServers);

        using var consumer = new ConsumerBuilder<string, string>(BuildConsumerConfig(options.Value.OcrConsumerGroupId)).Build();
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
                    logger.LogError(ex, "Paper OCR Kafka consume failed. Reason={Reason}", ex.Error.Reason);
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
                        "Paper OCR Kafka message skipped because payload is invalid. Topic={Topic}, Partition={Partition}, Offset={Offset}",
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
            logger.LogInformation("Paper OCR Kafka consumer stopped.");
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
            "Paper OCR Kafka message processing. PaperId={PaperId}, PaperVersionId={PaperVersionId}",
            message.PaperId,
            message.PaperVersionId);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
        var nougat = scope.ServiceProvider.GetRequiredService<INougatService>();
        var auditLog = scope.ServiceProvider.GetRequiredService<IAuditLogService>();
        var processingTracker = scope.ServiceProvider.GetRequiredService<IPaperProcessingTrackerService>();

        var version = await db.PaperVersions
            .FirstOrDefaultAsync(x => x.Id == message.PaperVersionId && x.PaperId == message.PaperId, cancellationToken);

        if (version is null)
        {
            logger.LogWarning(
                "Paper OCR Kafka message skipped because PaperVersion was not found. PaperId={PaperId}, PaperVersionId={PaperVersionId}",
                message.PaperId,
                message.PaperVersionId);
            return;
        }

        if (version.ConversionStatus == ConversionStatus.Completed)
        {
            logger.LogInformation(
                "Paper OCR Kafka message skipped because OCR is already completed. PaperVersionId={PaperVersionId}",
                version.Id);
            return;
        }

        try
        {
            version.ConversionStatus = ConversionStatus.Processing;
            version.ConversionError = null;
            await db.SaveChangesAsync(cancellationToken);
            await processingTracker.RecordStepStartedAsync(
                version.Id,
                ProcessingStage.OCR_REQUESTED,
                "NougatMarkdownConversionStarted",
                cancellationToken: cancellationToken);

            await auditLog.StartStepAsync(
                ProcessingStep.NOUGAT_MARKDOWN_CONVERSION,
                "Convert PDF to Markdown with Nougat",
                paperId: version.PaperId,
                paperVersionId: version.Id,
                correlationId: message.CorrelationId,
                message: "Nougat PDF to Markdown conversion started.",
                metadata: new { version.PdfS3Key, version.OriginalFileName },
                cancellationToken: cancellationToken);

            await using var pdfStream = await storage.DownloadAsync(version.PdfS3Key, cancellationToken);
            var markdown = await nougat.ConvertPdfToMarkdownAsync(
                pdfStream,
                version.OriginalFileName,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(markdown))
            {
                throw new AppException(NougatErrorCode.InvalidResponse);
            }

            var markdownKey = BuildMarkdownS3Key(version.OriginalFileName);
            await using var markdownStream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));
            await storage.UploadAsync(
                markdownStream,
                markdownKey,
                "text/markdown; charset=utf-8",
                cancellationToken);

            version.MarkdownS3Key = markdownKey;
            version.ConversionStatus = ConversionStatus.Completed;
            version.ConvertedAt = DateTime.UtcNow;
            version.ConversionError = null;
            await db.SaveChangesAsync(cancellationToken);
            await processingTracker.RecordStepCompletedAsync(
                version.Id,
                ProcessingStage.OCR_COMPLETED,
                "NougatMarkdownConversionCompleted",
                $"{{\"markdownS3Key\":\"{markdownKey}\",\"markdownLength\":{markdown.Length}}}",
                cancellationToken);

            await auditLog.CompleteStepAsync(
                ProcessingStep.NOUGAT_MARKDOWN_CONVERSION,
                "Convert PDF to Markdown with Nougat",
                paperId: version.PaperId,
                paperVersionId: version.Id,
                correlationId: message.CorrelationId,
                message: "Nougat PDF to Markdown conversion completed.",
                metadata: new { markdownS3Key = markdownKey, markdownLength = markdown.Length },
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Paper OCR Kafka processing completed. PaperId={PaperId}, PaperVersionId={PaperVersionId}, MarkdownS3Key={MarkdownS3Key}, ElapsedMs={ElapsedMs}",
                message.PaperId,
                version.Id,
                markdownKey,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Paper OCR Kafka processing failed. PaperId={PaperId}, PaperVersionId={PaperVersionId}, ElapsedMs={ElapsedMs}",
                message.PaperId,
                version.Id,
                stopwatch.ElapsedMilliseconds);

            version.ConversionStatus = ConversionStatus.Failed;
            version.ConversionError = ex.Message;
            await db.SaveChangesAsync(CancellationToken.None);
            await processingTracker.RecordStepFailedAsync(
                version.Id,
                ProcessingStage.FAILED,
                "NougatMarkdownConversionFailed",
                ex.Message,
                cancellationToken: CancellationToken.None);
            await auditLog.FailStepAsync(
                ProcessingStep.NOUGAT_MARKDOWN_CONVERSION,
                "Convert PDF to Markdown with Nougat",
                ex.Message,
                paperId: version.PaperId,
                paperVersionId: version.Id,
                correlationId: message.CorrelationId,
                message: "Nougat PDF to Markdown conversion failed.",
                metadata: new { version.PdfS3Key, version.OriginalFileName },
                cancellationToken: CancellationToken.None);
        }
    }

    private static string BuildMarkdownS3Key(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        return $"papers/markdown/{Guid.NewGuid():N}-{safeName}.md";
    }
}
