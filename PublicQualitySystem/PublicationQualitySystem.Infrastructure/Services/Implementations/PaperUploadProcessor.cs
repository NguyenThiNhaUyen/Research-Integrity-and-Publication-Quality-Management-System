using PublicationQualitySystem.Application.DTOs.File;
using PublicationQualitySystem.Application.DTOs.Paper;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Configurations;
using PublicationQualitySystem.Infrastructure.Security;
using PublicationQualitySystem.Shared.Exceptions;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public class PaperUploadProcessor(
    ApplicationDbContext db,
    ICurrentUserProvider currentUser,
    IPdfOcrService ocrService,
    ILogger<PaperUploadProcessor> logger) : IPaperUploadProcessor
{
    public async Task<PaperUploadProcessingResult> CreatePaperFromUploadedFileAsync(
        UploadedFile uploadedFile,
        FileUploadRequest file,
        CancellationToken cancellationToken = default)
    {
        var objectKey = string.IsNullOrWhiteSpace(uploadedFile.S3Key)
            ? uploadedFile.FileKey
            : uploadedFile.S3Key;
        var fallbackTitle = ExtractTitle(file.FileName);
        var metadata = await ExtractMetadataSafelyAsync(objectKey, fallbackTitle, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var paper = new Paper
        {
            PaperCode = GeneratePaperCode(uploadedFile.Id),
            Title = metadata.Title,
            AbstractText = metadata.AbstractText,
            Keywords = metadata.Keywords,
            ResearchField = metadata.ResearchField,
            FileUrl = uploadedFile.Url,
            FileType = uploadedFile.ContentType,
            S3Bucket = uploadedFile.S3Bucket,
            S3Key = objectKey,
            OwnerUserId = currentUser.Subject ?? throw new AppException(UploadFileErrorCode.UploadForbidden),
            SubmissionStatus = SubmissionStatus.DRAFT,
            CurrentVersion = 1
        };

        await db.Papers.AddAsync(paper, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var version = new PaperVersion
        {
            PaperId = paper.Id,
            Paper = paper,
            UploadedFileId = uploadedFile.Id,
            UploadedFile = uploadedFile,
            VersionNumber = 1,
            VersionName = $"Version 1 - {uploadedFile.OriginalFileName}",
            ChangeLog = $"Initial version created from uploaded file {uploadedFile.OriginalFileName}",
            FileUrl = uploadedFile.Url,
            FileType = Path.GetExtension(uploadedFile.OriginalFileName),
            OriginalFileName = uploadedFile.OriginalFileName,
            FileKey = uploadedFile.FileKey,
            ContentType = uploadedFile.ContentType,
            Size = uploadedFile.Size,
            UploadedBy = currentUser.Subject
        };

        await db.PaperVersions.AddAsync(version, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return new PaperUploadProcessingResult
        {
            PaperId = paper.Id,
            PaperCode = paper.PaperCode,
            Title = paper.Title,
            AbstractText = paper.AbstractText,
            Keywords = paper.Keywords,
            ResearchField = paper.ResearchField,
            CurrentVersion = paper.CurrentVersion,
            SubmissionStatus = paper.SubmissionStatus
        };
    }

    private static string GeneratePaperCode(long sequence) =>
        $"PAPER-{DateTime.UtcNow:yyyy}-{sequence:D6}";

    private async Task<ExtractedPaperMetadata> ExtractMetadataSafelyAsync(
        string objectKey,
        string fallbackTitle,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var rawText = await ocrService.ExtractTextAsync(objectKey);
            return ParseMetadata(rawText, fallbackTitle);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "OCR metadata extraction failed. Paper upload will continue with fallback metadata. objectKey={ObjectKey}",
                objectKey);

            return CreateFallbackMetadata(fallbackTitle);
        }
    }

    private static ExtractedPaperMetadata ParseMetadata(string? rawText, string fallbackTitle)
    {
        var lines = NormalizeLines(rawText).ToList();
        var title = ExtractMetadataTitle(lines);
        var abstractText = ExtractSection(lines, IsAbstractHeading, IsAbstractStopHeading);
        var keywords = ExtractSection(lines, IsKeywordsHeading, IsKeywordsStopHeading);
        var researchField = InferResearchField(title, abstractText, keywords);

        return new ExtractedPaperMetadata(
            NormalizeRequired(title, fallbackTitle),
            NormalizeOptional(abstractText),
            NormalizeOptional(keywords),
            NormalizeRequired(researchField, "Unclassified"));
    }

    private static IEnumerable<string> NormalizeLines(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            yield break;
        }

        using var reader = new StringReader(rawText);
        while (reader.ReadLine() is { } line)
        {
            var normalized = string.Join(' ', line.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim();
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                yield return normalized;
            }
        }
    }

    private static string? ExtractMetadataTitle(IReadOnlyList<string> lines)
    {
        foreach (var line in lines.TakeWhile(line => !IsAbstractHeading(line)))
        {
            if (IsMeaningfulTitleLine(line))
            {
                return line;
            }
        }

        return null;
    }

    private static string? ExtractSection(
        IReadOnlyList<string> lines,
        Func<string, bool> isStartHeading,
        Func<string, bool> isStopHeading)
    {
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            if (!isStartHeading(line))
            {
                continue;
            }

            var sectionLines = new List<string>();
            var inlineText = ExtractInlineHeadingText(line);
            if (!string.IsNullOrWhiteSpace(inlineText))
            {
                sectionLines.Add(inlineText);
            }

            for (var sectionIndex = index + 1; sectionIndex < lines.Count; sectionIndex++)
            {
                var sectionLine = lines[sectionIndex];
                if (isStopHeading(sectionLine))
                {
                    break;
                }

                sectionLines.Add(sectionLine);
            }

            return sectionLines.Count == 0 ? null : string.Join(' ', sectionLines);
        }

        return null;
    }

    private static string? ExtractInlineHeadingText(string line)
    {
        var separatorIndex = line.IndexOf(':');
        if (separatorIndex < 0 || separatorIndex == line.Length - 1)
        {
            return null;
        }

        return line[(separatorIndex + 1)..].Trim();
    }

    private static string InferResearchField(string? title, string? abstractText, string? keywords)
    {
        var text = string.Join(' ', new[] { title, abstractText, keywords }
            .Where(value => !string.IsNullOrWhiteSpace(value)));

        if (ContainsAny(text, "artificial intelligence", "machine learning", "deep learning", "neural network", " ai "))
        {
            return "Artificial Intelligence";
        }

        if (ContainsAny(text, "natural language processing", " nlp ", "language model", "text mining"))
        {
            return "NLP";
        }

        if (ContainsAny(text, "computer vision", "image recognition", "object detection", "image classification"))
        {
            return "Computer Vision";
        }

        if (ContainsAny(text, "cyber security", "cybersecurity", "information security", "network security"))
        {
            return "Cyber Security";
        }

        if (ContainsAny(text, "software engineering", "software development", "software testing", "code quality"))
        {
            return "Software Engineering";
        }

        if (ContainsAny(text, "data science", "data analytics", "big data", "data mining"))
        {
            return "Data Science";
        }

        if (ContainsAny(text, "internet of things", " iot ", "sensor network"))
        {
            return "IoT";
        }

        if (ContainsAny(text, "blockchain", "smart contract", "distributed ledger"))
        {
            return "Blockchain";
        }

        if (ContainsAny(text, "cloud computing", "cloud infrastructure", "edge computing"))
        {
            return "Cloud Computing";
        }

        return "Unclassified";
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        var searchable = $" {text} ";
        return keywords.Any(keyword => searchable.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsMeaningfulTitleLine(string line)
    {
        if (line.Length < 8 || line.Length > 250)
        {
            return false;
        }

        if (line.Count(char.IsLetter) < 5)
        {
            return false;
        }

        return !ContainsAny(
            line,
            " doi:",
            "http://",
            "https://",
            "www.",
            "@",
            "journal ",
            "conference ",
            "proceedings ",
            "volume ",
            "vol. ",
            "issue ",
            "issn ",
            "isbn ",
            "page ",
            " pp. ");
    }

    private static bool IsAbstractHeading(string line) =>
        IsHeading(line, "abstract");

    private static bool IsKeywordsHeading(string line) =>
        IsHeading(line, "keywords") || IsHeading(line, "index terms");

    private static bool IsAbstractStopHeading(string line) =>
        IsKeywordsHeading(line) || IsIntroductionHeading(line);

    private static bool IsKeywordsStopHeading(string line) =>
        IsIntroductionHeading(line);

    private static bool IsIntroductionHeading(string line) =>
        IsHeading(line, "introduction") || IsHeading(line, "1. introduction");

    private static bool IsHeading(string line, string heading)
    {
        var normalized = line.Trim().Trim('-', '.', ':').ToLowerInvariant();
        return normalized == heading || normalized.StartsWith($"{heading}:", StringComparison.OrdinalIgnoreCase);
    }

    private static ExtractedPaperMetadata CreateFallbackMetadata(string fallbackTitle) =>
        new(NormalizeRequired(null, fallbackTitle), null, null, "Unclassified");

    private static string NormalizeRequired(string? value, string fallback)
    {
        var normalized = NormalizeOptional(value);
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            return normalized;
        }

        return string.IsNullOrWhiteSpace(fallback) ? "Untitled manuscript" : fallback.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string ExtractTitle(string fileName)
    {
        var title = Path.GetFileNameWithoutExtension(fileName);
        return string.IsNullOrWhiteSpace(title) ? "Untitled manuscript" : title.Trim();
    }

    private sealed record ExtractedPaperMetadata(
        string Title,
        string? AbstractText,
        string? Keywords,
        string? ResearchField);
}
