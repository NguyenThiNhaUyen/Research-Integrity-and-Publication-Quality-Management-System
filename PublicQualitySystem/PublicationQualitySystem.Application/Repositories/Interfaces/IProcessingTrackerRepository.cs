using PublicationQualitySystem.Domain.Entities;

namespace PublicationQualitySystem.Application.Repositories.Interfaces;

public interface IProcessingTrackerRepository
{
    Task<PaperProcessingTracker?> FindByPaperVersionIdAsync(long paperVersionId, CancellationToken cancellationToken = default);
}
