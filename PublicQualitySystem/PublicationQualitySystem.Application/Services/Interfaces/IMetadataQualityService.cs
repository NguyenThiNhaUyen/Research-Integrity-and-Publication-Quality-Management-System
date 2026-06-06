using PublicationQualitySystem.Application.DTOs.Metadata;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IMetadataQualityService
{
    Task<MetadataQualityResponse> GetByPaperVersionIdAsync(long paperVersionId, CancellationToken cancellationToken);
}
