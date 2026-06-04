using System.Diagnostics;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using PublicationQualitySystem.Application.DTOs.Paper;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Configurations;
using PublicationQualitySystem.Shared.Exceptions;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public sealed class PaperOcrBackgroundService(
    IPaperOcrQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<PaperOcrBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Paper OCR background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            PaperOcrJob job;
            try
            {
                job = await queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await ProcessJobAsync(job, stoppingToken);
        }

        logger.LogInformation("Paper OCR background service stopped.");
    }

    private async Task ProcessJobAsync(PaperOcrJob job, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation(
            "Paper OCR job processing. PaperVersionId={PaperVersionId}",
            job.PaperVersionId);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
        var nougat = scope.ServiceProvider.GetRequiredService<INougatService>();
        var auditLog = scope.ServiceProvider.GetRequiredService<IAuditLogService>();
        var processingTracker = scope.ServiceProvider.GetRequiredService<IPaperProcessingTrackerService>();

        var version = await db.PaperVersions
            .Include(x => x.Paper)
            .FirstOrDefaultAsync(x => x.Id == job.PaperVersionId, cancellationToken);

        if (version is null)
        {
            logger.LogWarning(
                "Paper OCR job skipped because PaperVersion was not found. PaperVersionId={PaperVersionId}",
                job.PaperVersionId);
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
                message: "Nougat PDF to Markdown conversion completed.",
                metadata: new { markdownS3Key = markdownKey, markdownLength = markdown.Length },
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Paper OCR job completed. PaperVersionId={PaperVersionId}, MarkdownS3Key={MarkdownS3Key}, ElapsedMs={ElapsedMs}",
                version.Id,
                markdownKey,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Paper OCR job failed. PaperVersionId={PaperVersionId}, ElapsedMs={ElapsedMs}",
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
