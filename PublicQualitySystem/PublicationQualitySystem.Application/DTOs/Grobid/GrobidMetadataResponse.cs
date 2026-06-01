namespace PublicationQualitySystem.Application.DTOs.Grobid;

public class GrobidMetadataResponse
{
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
    public string MetadataSource { get; set; } = "GROBID";
    public string? DoiSource { get; set; }
    public string? JournalSource { get; set; }
    public IReadOnlyList<string> Keywords { get; set; } = Array.Empty<string>();
    public IReadOnlyList<ReferenceDto> References { get; set; } = Array.Empty<ReferenceDto>();
    public string RawGrobidXml { get; set; } = string.Empty;
}
