using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Application.DTOs.Paper;

public class PaperResponse
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int CurrentVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public PaperVersionSummaryResponse? CurrentVersionInfo { get; set; }
}

public class PaperVersionSummaryResponse
{
    public long PaperVersionId { get; set; }
    public int VersionNumber { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string PdfS3Key { get; set; } = string.Empty;
    public string? MarkdownS3Key { get; set; }
    public ConversionStatus ConversionStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConvertedAt { get; set; }
}
