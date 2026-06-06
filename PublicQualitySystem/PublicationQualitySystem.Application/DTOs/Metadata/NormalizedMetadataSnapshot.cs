using PublicationQualitySystem.Application.DTOs.Grobid;

namespace PublicationQualitySystem.Application.DTOs.Metadata;

public class NormalizedMetadataSnapshot
{
    public string? Title { get; set; }
    public string? Doi { get; set; }
    public string? Journal { get; set; }
    public string? Publisher { get; set; }
    public string? Venue { get; set; }
    public int? PublicationYear { get; set; }
    public string? Volume { get; set; }
    public string? Issue { get; set; }
    public string? Pages { get; set; }
    public DateOnly? ReceivedDate { get; set; }
    public DateOnly? RevisedDate { get; set; }
    public DateOnly? AcceptedDate { get; set; }
    public DateOnly? PublishedDate { get; set; }
    public string? OpenAccessLicense { get; set; }
    public IReadOnlyList<string> Keywords { get; set; } = Array.Empty<string>();
    public IReadOnlyList<ReferenceDto> References { get; set; } = Array.Empty<ReferenceDto>();
}
