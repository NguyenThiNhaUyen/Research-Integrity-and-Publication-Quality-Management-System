using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using PublicationQualitySystem.Application.DTOs.Paper;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Configurations;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public sealed class PaperMetadataBackgroundService(
    IPaperMetadataQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<PaperMetadataBackgroundService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Paper metadata background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            PaperMetadataExtractionJob job;
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

        logger.LogInformation("Paper metadata background service stopped.");
    }

    private async Task ProcessJobAsync(PaperMetadataExtractionJob job, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation(
            "Metadata extraction started. PaperId={PaperId}, PaperVersionId={PaperVersionId}",
            job.PaperId,
            job.PaperVersionId);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
        var grobid = scope.ServiceProvider.GetRequiredService<IGrobidService>();
        var crossref = scope.ServiceProvider.GetRequiredService<ICrossrefService>();

        var version = await db.PaperVersions
            .Include(x => x.Paper)
            .ThenInclude(x => x.Metadata)
            .FirstOrDefaultAsync(x => x.Id == job.PaperVersionId && x.PaperId == job.PaperId, cancellationToken);

        if (version is null)
        {
            logger.LogWarning(
                "Metadata extraction skipped because PaperVersion was not found. PaperId={PaperId}, PaperVersionId={PaperVersionId}",
                job.PaperId,
                job.PaperVersionId);
            return;
        }

        var metadata = version.Paper.Metadata;
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
                "Metadata saved. PaperId={PaperId}, PaperVersionId={PaperVersionId}, Status={Status}, ElapsedMs={ElapsedMs}",
                version.PaperId,
                version.Id,
                metadata.ExtractionStatus,
                stopwatch.ElapsedMilliseconds);

            logger.LogInformation(
                "Metadata extraction completed. PaperId={PaperId}, PaperVersionId={PaperVersionId}, ElapsedMs={ElapsedMs}",
                version.PaperId,
                version.Id,
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
