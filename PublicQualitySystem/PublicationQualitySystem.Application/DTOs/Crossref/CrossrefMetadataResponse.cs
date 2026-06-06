using PublicationQualitySystem.Application.DTOs.Grobid;

namespace PublicationQualitySystem.Application.DTOs.Crossref;

public class CrossrefMetadataResponse
{
    public string? Title { get; set; }
    public string? Abstract { get; set; }
    public string? Doi { get; set; }
    public string? Journal { get; set; }
    public string? Publisher { get; set; }
    public int? PublicationYear { get; set; }
    public string? Volume { get; set; }
    public string? Issue { get; set; }
    public string? Pages { get; set; }
    public DateOnly? PublishedDate { get; set; }
    public IReadOnlyList<AuthorDto> Authors { get; set; } = Array.Empty<AuthorDto>();
}
