using PublicationQualitySystem.Domain.Entities;

namespace PublicationQualitySystem.Application.Repositories.Interfaces;

public interface IPaperVersionRepository
{
    Task<IReadOnlyList<PaperVersion>> FindByPaperIdAsync(long paperId, CancellationToken cancellationToken = default);

    Task<PaperVersion?> FindByIdAsync(long paperVersionId, CancellationToken cancellationToken = default);
}
