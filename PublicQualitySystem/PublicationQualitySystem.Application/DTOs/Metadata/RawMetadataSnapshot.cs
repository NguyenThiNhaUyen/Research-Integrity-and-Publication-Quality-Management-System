using PublicationQualitySystem.Application.DTOs.Crossref;
using PublicationQualitySystem.Application.DTOs.Grobid;

namespace PublicationQualitySystem.Application.DTOs.Metadata;

public class RawMetadataSnapshot
{
    public GrobidMetadataResponse? Grobid { get; set; }
    public CrossrefMetadataResponse? Crossref { get; set; }
    public string? MetadataSource { get; set; }
}
