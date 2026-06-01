using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Application.DTOs.Grobid;
using PublicationQualitySystem.Application.DTOs.IntegrationEvents;
using PublicationQualitySystem.Application.DTOs.Paper;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Configurations;
using PublicationQualitySystem.Infrastructure.Options;
using Microsoft.Extensions.Options;
using PublicationQualitySystem.Shared.Exceptions;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public class PaperService(
    ApplicationDbContext db,
    IFileStorageService storage,
    IOptions<KafkaOptions> kafkaOptions,
    ILogger<PaperService> logger) : IPaperService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PaperVersionResponse> UploadPaperAsync(
        Stream pdfStream,
        string fileName,
        string contentType,
        string? title,
        CancellationToken cancellationToken)
    {
        var uploadId = Guid.NewGuid().ToString("N");
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation(
            "Paper upload started. UploadId={UploadId}, FileName={FileName}, ContentType={ContentType}, HasTitle={HasTitle}",
            uploadId,
            fileName,
            contentType,
            !string.IsNullOrWhiteSpace(title));

        if (!IsPdf(fileName, contentType))
        {
            logger.LogWarning(
                "Paper upload rejected because file is not a PDF. UploadId={UploadId}, FileName={FileName}, ContentType={ContentType}",
                uploadId,
                fileName,
                contentType);
            throw new AppException(PaperErrorCode.InvalidPdf);
        }

        await using var buffer = new MemoryStream();
        logger.LogInformation("Copying uploaded PDF stream to memory. UploadId={UploadId}", uploadId);
        await pdfStream.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length == 0)
        {
            logger.LogWarning("Paper upload rejected because file is empty. UploadId={UploadId}", uploadId);
            throw new AppException(PaperErrorCode.EmptyFile);
        }

        var pdfBytes = buffer.ToArray();
        logger.LogInformation(
            "Uploaded PDF stream buffered. UploadId={UploadId}, Bytes={Bytes}, ElapsedMs={ElapsedMs}",
            uploadId,
            pdfBytes.Length,
            stopwatch.ElapsedMilliseconds);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var paper = new Paper
        {
            Title = string.IsNullOrWhiteSpace(title) ? Path.GetFileNameWithoutExtension(fileName) : title.Trim()
        };
        db.Papers.Add(paper);
        logger.LogInformation("Saving Paper entity. UploadId={UploadId}, Title={Title}", uploadId, paper.Title);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Paper entity saved. UploadId={UploadId}, PaperId={PaperId}, ElapsedMs={ElapsedMs}",
            uploadId,
            paper.Id,
            stopwatch.ElapsedMilliseconds);
        
        var version = new PaperVersion
        {
            PaperId = paper.Id,
            VersionNumber = 1,
            OriginalFileName = fileName,
            PdfS3Key = BuildS3Key("papers/pdf", paper.Id, fileName),
            ConversionStatus = ConversionStatus.Pending
        };

        logger.LogInformation(
            "Uploading PDF to S3. UploadId={UploadId}, PaperId={PaperId}, PdfS3Key={PdfS3Key}, Bytes={Bytes}",
            uploadId,
            paper.Id,
            version.PdfS3Key,
            pdfBytes.Length);
        await using (var pdfUploadStream = new MemoryStream(pdfBytes))
        {
            await storage.UploadAsync(pdfUploadStream, version.PdfS3Key, "application/pdf", cancellationToken);
        }
        logger.LogInformation(
            "PDF uploaded to S3. UploadId={UploadId}, PdfS3Key={PdfS3Key}, ElapsedMs={ElapsedMs}",
            uploadId,
            version.PdfS3Key,
            stopwatch.ElapsedMilliseconds);

        db.PaperVersions.Add(version);
        paper.CurrentVersion = version.VersionNumber;
        logger.LogInformation("Saving PaperVersion entity. UploadId={UploadId}, PaperId={PaperId}", uploadId, paper.Id);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "PaperVersion entity saved. UploadId={UploadId}, PaperVersionId={PaperVersionId}, ElapsedMs={ElapsedMs}",
            uploadId,
            version.Id,
            stopwatch.ElapsedMilliseconds);

        var metadata = new PaperMetadata
        {
            PaperId = paper.Id,
            ExtractionStatus = MetadataExtractionStatus.Pending
        };
        db.PaperMetadata.Add(metadata);
        logger.LogInformation(
            "Creating PaperMetadata placeholder. UploadId={UploadId}, PaperId={PaperId}, PaperVersionId={PaperVersionId}",
            uploadId,
            paper.Id,
            version.Id);
        
        var integrationEvent = new PaperUploadedIntegrationEvent
        {
            PaperId = paper.Id,
            PaperVersionId = version.Id,
            PdfS3Key = version.PdfS3Key,
            OriginalFileName = version.OriginalFileName,
            UploadedAt = DateTime.UtcNow
        };

        db.OutboxMessages.Add(new OutboxMessage
        {
            Topic = kafkaOptions.Value.PaperUploadedTopic,
            Key = paper.Id.ToString(),
            Type = nameof(PaperUploadedIntegrationEvent),
            Payload = JsonSerializer.Serialize(integrationEvent, JsonOptions),
            Status = OutboxMessageStatus.Pending
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Paper uploaded integration event saved to outbox. UploadId={UploadId}, PaperId={PaperId}, PaperVersionId={PaperVersionId}, Topic={Topic}, ElapsedMs={ElapsedMs}",
            uploadId,
            paper.Id,
            version.Id,
            kafkaOptions.Value.PaperUploadedTopic,
            stopwatch.ElapsedMilliseconds);

        logger.LogInformation(
            "Paper upload finished. UploadId={UploadId}, PaperId={PaperId}, PaperVersionId={PaperVersionId}, ConversionStatus={ConversionStatus}, ElapsedMs={ElapsedMs}",
            uploadId,
            paper.Id,
            version.Id,
            version.ConversionStatus,
            stopwatch.ElapsedMilliseconds);

        return ToResponse(paper, version);
    }

    public async Task<PaperMetadataResponse> GetMetadataAsync(
        long paperId,
        CancellationToken cancellationToken)
    {
        var paperExists = await db.Papers.AnyAsync(x => x.Id == paperId, cancellationToken);
        if (!paperExists)
        {
            throw new AppException(PaperErrorCode.NotFound);
        }

        var metadata = await db.PaperMetadata
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PaperId == paperId, cancellationToken);

        if (metadata is null)
        {
            throw new AppException(PaperErrorCode.MetadataNotFound);
        }

        return new PaperMetadataResponse
        {
            PaperId = metadata.PaperId,
            Title = metadata.Title,
            Authors = DeserializeJson<IReadOnlyList<AuthorDto>>(metadata.AuthorsJson) ?? Array.Empty<AuthorDto>(),
            Abstract = metadata.Abstract,
            Doi = metadata.Doi,
            ArxivId = metadata.ArxivId,
            Journal = metadata.Journal,
            Publisher = metadata.Publisher,
            Venue = metadata.Venue,
            ConferenceName = metadata.ConferenceName,
            PublicationYear = metadata.PublicationYear,
            Volume = metadata.Volume,
            Issue = metadata.Issue,
            Pages = metadata.Pages,
            CorrespondingAuthor = metadata.CorrespondingAuthor,
            MetadataSource = metadata.MetadataSource,
            DoiSource = metadata.DoiSource,
            JournalSource = metadata.JournalSource,
            Keywords = DeserializeJson<IReadOnlyList<string>>(metadata.KeywordsJson) ?? Array.Empty<string>(),
            References = DeserializeJson<IReadOnlyList<ReferenceDto>>(metadata.ReferencesJson) ?? Array.Empty<ReferenceDto>(),
            ExtractionStatus = metadata.ExtractionStatus,
            ExtractedAt = metadata.ExtractedAt,
            ExtractionError = metadata.ExtractionError
        };
    }

    private static bool IsPdf(string fileName, string contentType) =>
        Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase)
        && contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);

    private static string BuildS3Key(string folder, long paperId, string fileName)
    {
        var safeName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        return $"{folder}/{paperId}/{Guid.NewGuid():N}-{safeName}";
    }

    private static PaperVersionResponse ToResponse(Paper paper, PaperVersion version) => new()
    {
        PaperId = paper.Id,
        PaperVersionId = version.Id,
        Title = paper.Title,
        VersionNumber = version.VersionNumber,
        OriginalFileName = version.OriginalFileName,
        PdfS3Key = version.PdfS3Key,
        MarkdownS3Key = version.MarkdownS3Key,
        ConversionStatus = version.ConversionStatus,
        ConvertedAt = version.ConvertedAt,
        ConversionError = version.ConversionError
    };

    private static T? DeserializeJson<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
