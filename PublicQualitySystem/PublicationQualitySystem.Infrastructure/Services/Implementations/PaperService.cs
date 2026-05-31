using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Application.DTOs.Paper;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Configurations;
using PublicationQualitySystem.Shared.Exceptions;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public class PaperService(
    ApplicationDbContext db,
    IFileStorageService storage,
    IPaperOcrQueue paperOcrQueue,
    ILogger<PaperService> logger) : IPaperService
{
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

        version.ConversionStatus = ConversionStatus.Processing;
        version.ConversionError = null;
        logger.LogInformation(
            "Marking conversion as Processing. UploadId={UploadId}, PaperVersionId={PaperVersionId}",
            uploadId,
            version.Id);
        await db.SaveChangesAsync(cancellationToken);

        await paperOcrQueue.QueueAsync(new PaperOcrJob(version.Id), cancellationToken);
        logger.LogInformation(
            "Paper OCR job queued. UploadId={UploadId}, PaperId={PaperId}, PaperVersionId={PaperVersionId}, PdfS3Key={PdfS3Key}, ElapsedMs={ElapsedMs}",
            uploadId,
            paper.Id,
            version.Id,
            version.PdfS3Key,
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
}
