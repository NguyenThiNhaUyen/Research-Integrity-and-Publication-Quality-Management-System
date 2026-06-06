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
    public DateOnly? ReceivedDate { get; set; }
    public DateOnly? RevisedDate { get; set; }
    public DateOnly? AcceptedDate { get; set; }
    public DateOnly? PublishedDate { get; set; }
    public string? OpenAccessLicense { get; set; }
    public string? MetadataSource { get; set; }
    public string? DoiSource { get; set; }
    public string? JournalSource { get; set; }
    public string KeywordsJson { get; set; } = "[]";
    public string FundingOrganizationsJson { get; set; } = "[]";
    public string AuthorsJson { get; set; } = "[]";
    public string ReferencesJson { get; set; } = "[]";
    public string? RawMetadataJson { get; set; }
    public string? NormalizedMetadataJson { get; set; }
    public int? MetadataCleanlinessScore { get; set; }
    public int? ReferenceCleanlinessScore { get; set; }
    public int? DirtyFieldCount { get; set; }
    public string? MetadataIssueCodesJson { get; set; }
    public string? MetadataWarningsJson { get; set; }
    public string? RawGrobidXml { get; set; }
    public MetadataExtractionStatus ExtractionStatus { get; set; } = MetadataExtractionStatus.Pending;
    public string? ExtractionError { get; set; }
    public DateTime? ExtractedAt { get; set; }
    public int? MetadataQualityTotalScore { get; set; }
    public int? MetadataQualityCoreScore { get; set; }
    public int? MetadataQualityExtendedScore { get; set; }
    public int? MetadataQualityEnrichmentScore { get; set; }
    public string? MetadataQualityGrade { get; set; }
    public bool? MetadataQualityCanProceed { get; set; }
    public string? MetadataQualityMissingFieldsJson { get; set; }
    public string? MetadataQualityWarningsJson { get; set; }
    public string? MetadataQualityFieldScoresJson { get; set; }
    public DateTime? MetadataQualityScoredAt { get; set; }
}
