using PublicationQualitySystem.Application.DTOs.Paper;
using PublicationQualitySystem.Domain.Entities;

namespace PublicationQualitySystem.Application.Mappings;

public static class PaperVersionMapper
{
    public static PaperVersionResponse ToResponse(PaperVersion version) => new()
    {
        PaperId = version.PaperId,
        PaperVersionId = version.Id,
        Title = version.Paper?.Title ?? string.Empty,
        VersionNumber = version.VersionNumber,
        OriginalFileName = version.OriginalFileName,
        PdfS3Key = version.PdfS3Key,
        MarkdownS3Key = version.MarkdownS3Key,
        ConversionStatus = version.ConversionStatus,
        ConvertedAt = version.ConvertedAt,
        ConversionError = version.ConversionError
    };
}
