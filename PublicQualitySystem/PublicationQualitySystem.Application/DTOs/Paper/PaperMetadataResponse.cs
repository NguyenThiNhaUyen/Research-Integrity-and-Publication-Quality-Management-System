using PublicationQualitySystem.Application.DTOs.Grobid;
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
    public IReadOnlyList<string> Keywords { get; set; } = Array.Empty<string>();
    public IReadOnlyList<ReferenceDto> References { get; set; } = Array.Empty<ReferenceDto>();
    public MetadataExtractionStatus ExtractionStatus { get; set; }
    public DateTime? ExtractedAt { get; set; }
    public string? ExtractionError { get; set; }
}
