using PublicationQualitySystem.Application.DTOs.Paper;
using PublicationQualitySystem.Domain.Entities;

namespace PublicationQualitySystem.Application.Mappings;

public static class PaperMapper
{
    public static PaperResponse ToResponse(Paper paper)
    {
        var currentVersion = paper.Versions
            .OrderByDescending(x => x.VersionNumber)
            .FirstOrDefault(x => x.VersionNumber == paper.CurrentVersion)
            ?? paper.Versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();

        return new PaperResponse
        {
            Id = paper.Id,
            Title = paper.Title,
            CurrentVersion = paper.CurrentVersion,
            CreatedAt = paper.CreatedAt,
            UpdatedAt = paper.UpdatedAt,
            CreatedBy = paper.CreatedBy,
            CurrentVersionInfo = currentVersion is null ? null : ToVersionSummary(currentVersion)
        };
    }

    private static PaperVersionSummaryResponse ToVersionSummary(PaperVersion version) => new()
    {
        PaperVersionId = version.Id,
        VersionNumber = version.VersionNumber,
        OriginalFileName = version.OriginalFileName,
        PdfS3Key = version.PdfS3Key,
        MarkdownS3Key = version.MarkdownS3Key,
        ConversionStatus = version.ConversionStatus,
        CreatedAt = version.CreatedAt,
        ConvertedAt = version.ConvertedAt
    };
}
