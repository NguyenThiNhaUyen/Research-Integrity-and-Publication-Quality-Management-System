namespace PublicationQualitySystem.Application.DTOs.Grobid;

public class ReferenceDto
{
    public string? Title { get; set; }
    public IReadOnlyList<AuthorDto> Authors { get; set; } = Array.Empty<AuthorDto>();
    public string? Journal { get; set; }
    public string? Publisher { get; set; }
    public int? PublicationYear { get; set; }
    public string? RawText { get; set; }
    public string? Doi { get; set; }
}
