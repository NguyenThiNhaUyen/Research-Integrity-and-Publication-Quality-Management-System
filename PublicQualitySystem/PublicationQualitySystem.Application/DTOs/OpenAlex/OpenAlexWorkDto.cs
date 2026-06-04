using PublicationQualitySystem.Application.DTOs.Grobid;

namespace PublicationQualitySystem.Application.DTOs.OpenAlex;

public class OpenAlexWorkDto
{
    public string? Id { get; set; }
    public string? Doi { get; set; }
    public string? Title { get; set; }
    public int? PublicationYear { get; set; }
    public int? CitedByCount { get; set; }
    public IReadOnlyList<AuthorDto> Authors { get; set; } = Array.Empty<AuthorDto>();
    public string? Journal { get; set; }
    public string? Abstract { get; set; }
    public IReadOnlyList<string> ReferencedWorks { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Concepts { get; set; } = Array.Empty<string>();
    public string RawJson { get; set; } = "{}";
}
