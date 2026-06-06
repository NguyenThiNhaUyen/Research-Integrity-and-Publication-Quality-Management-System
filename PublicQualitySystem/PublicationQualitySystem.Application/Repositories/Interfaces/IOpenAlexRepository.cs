using PublicationQualitySystem.Domain.Entities;

namespace PublicationQualitySystem.Application.Repositories.Interfaces;

public interface IOpenAlexRepository
{
    Task<PaperSimilarityCheck?> FindSimilarityByPaperVersionIdAsync(long paperVersionId, CancellationToken cancellationToken = default);
}
