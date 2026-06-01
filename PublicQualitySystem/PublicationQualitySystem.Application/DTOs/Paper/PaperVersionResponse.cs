using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Application.DTOs.Paper;

public class PaperVersionResponse
{
    public long PaperId { get; set; }
    public long PaperVersionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string PdfS3Key { get; set; } = string.Empty;
    public string? MarkdownS3Key { get; set; }
    public ConversionStatus ConversionStatus { get; set; }
    public DateTime? ConvertedAt { get; set; }
    public string? ConversionError { get; set; }
}
