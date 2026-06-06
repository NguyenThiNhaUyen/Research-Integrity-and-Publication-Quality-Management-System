namespace PublicationQualitySystem.Application.DTOs.Grobid;

public class ReferenceDto
{
    public string? Title { get; set; }
    public IReadOnlyList<AuthorDto> Authors { get; set; } = Array.Empty<AuthorDto>();
    public string? Journal { get; set; }
    public string? Publisher { get; set; }
    public int? PublicationYear { get; set; }
    public string? Volume { get; set; }
    public string? Issue { get; set; }
    public string? Pages { get; set; }
    public string? RawText { get; set; }
    public string? Doi { get; set; }
    public string? DoiSource { get; set; }
    public string? DoiConfidence { get; set; }
    public IReadOnlyList<string> IssueCodes { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> WarningMessages { get; set; } = Array.Empty<string>();
}
