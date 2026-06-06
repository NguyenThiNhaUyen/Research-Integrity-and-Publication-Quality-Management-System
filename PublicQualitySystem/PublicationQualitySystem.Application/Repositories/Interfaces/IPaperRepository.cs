using PublicationQualitySystem.Domain.Entities;

namespace PublicationQualitySystem.Application.Repositories.Interfaces;

public interface IPaperRepository
{
    Task<IReadOnlyList<Paper>> FindPagedAsync(
        string? search,
        string? ownerUserId,
        bool includeAll,
        int skip,
        int take,
        bool sortDescending,
        CancellationToken cancellationToken = default);

    Task<Paper?> FindByIdAsync(long paperId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(long paperId, CancellationToken cancellationToken = default);
}
