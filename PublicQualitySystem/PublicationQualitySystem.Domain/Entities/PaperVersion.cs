using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Domain.Entities;

public class PaperVersion : BaseEntity
{
    public long PaperId { get; set; }
    public Paper Paper { get; set; } = null!;
    public int VersionNumber { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string PdfS3Key { get; set; } = string.Empty;
    public string? MarkdownS3Key { get; set; }
    public ConversionStatus ConversionStatus { get; set; } = ConversionStatus.Pending;
    public DateTime? ConvertedAt { get; set; }
    public string? ConversionError { get; set; }
}
