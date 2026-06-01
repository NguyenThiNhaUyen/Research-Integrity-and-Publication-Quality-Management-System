using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Domain.Entities;

public class PaperMetadata : BaseEntity
{
    public long PaperId { get; set; }
    public Paper Paper { get; set; } = null!;
    public string? Title { get; set; }
    public string? Abstract { get; set; }
    public string? Doi { get; set; }
    public string? ArxivId { get; set; }
    public string? Journal { get; set; }
    public string? Publisher { get; set; }
    public string? Venue { get; set; }
    public string? ConferenceName { get; set; }
    public int? PublicationYear { get; set; }
    public string? Volume { get; set; }
    public string? Issue { get; set; }
    public string? Pages { get; set; }
    public string? CorrespondingAuthor { get; set; }
    public string? MetadataSource { get; set; }
    public string? DoiSource { get; set; }
    public string? JournalSource { get; set; }
    public string KeywordsJson { get; set; } = "[]";
    public string AuthorsJson { get; set; } = "[]";
    public string ReferencesJson { get; set; } = "[]";
    public string? RawGrobidXml { get; set; }
    public MetadataExtractionStatus ExtractionStatus { get; set; } = MetadataExtractionStatus.Pending;
    public string? ExtractionError { get; set; }
    public DateTime? ExtractedAt { get; set; }
}
