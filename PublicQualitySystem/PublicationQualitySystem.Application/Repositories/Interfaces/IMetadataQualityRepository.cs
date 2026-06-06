using PublicationQualitySystem.Domain.Entities;

namespace PublicationQualitySystem.Application.Repositories.Interfaces;

public interface IMetadataQualityRepository
{
    Task<PaperMetadata?> FindByPaperVersionIdAsync(long paperVersionId, CancellationToken cancellationToken = default);
}
