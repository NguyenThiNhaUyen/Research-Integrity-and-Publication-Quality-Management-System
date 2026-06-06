using PublicationQualitySystem.Application.DTOs.Grobid;
using PublicationQualitySystem.Application.DTOs.Metadata;
using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Application.DTOs.Paper;

public class PaperMetadataResponse
{
    public long PaperId { get; set; }
    public string? Title { get; set; }
    public IReadOnlyList<AuthorDto> Authors { get; set; } = Array.Empty<AuthorDto>();
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
    public RawMetadataSnapshot? RawMetadata { get; set; }
    public NormalizedMetadataSnapshot? NormalizedMetadata { get; set; }
    public int? MainMetadataCleanlinessScore { get; set; }
    public int? ReferenceCleanlinessScore { get; set; }
    public int? DirtyFieldCount { get; set; }
    public IReadOnlyList<string> IssueCodes { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> WarningMessages { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Keywords { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> FundingOrganizations { get; set; } = Array.Empty<string>();
    public IReadOnlyList<ReferenceDto> References { get; set; } = Array.Empty<ReferenceDto>();
    public MetadataExtractionStatus ExtractionStatus { get; set; }
    public DateTime? ExtractedAt { get; set; }
    public string? ExtractionError { get; set; }
    public MetadataQualityScoreResponse? MetadataQuality { get; set; }
}
