using PublicationQualitySystem.Application.DTOs.Crossref;
using PublicationQualitySystem.Application.DTOs.Grobid;
using PublicationQualitySystem.Application.DTOs.Metadata;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IMetadataNormalizerService
{
    MetadataNormalizationResult Normalize(
        GrobidMetadataResponse metadata,
        CrossrefMetadataResponse? crossrefMetadata = null);
}
