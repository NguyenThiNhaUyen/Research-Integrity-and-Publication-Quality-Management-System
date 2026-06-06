using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Application.Repositories.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Infrastructure.Configurations;

namespace PublicationQualitySystem.Infrastructure.Repositories.Implementations;

public class PaperRepository(ApplicationDbContext db) : IPaperRepository
{
    public async Task<IReadOnlyList<Paper>> FindPagedAsync(
        string? search,
        string? ownerUserId,
        bool includeAll,
        int skip,
        int take,
        bool sortDescending,
        CancellationToken cancellationToken = default)
    {
        var query = db.Papers
            .AsNoTracking()
            .Include(x => x.Versions)
            .Where(x => !x.Deleted);

        if (!includeAll)
        {
            query = query.Where(x => !string.IsNullOrWhiteSpace(ownerUserId) && x.CreatedBy == ownerUserId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x => EF.Functions.ILike(x.Title, $"%{keyword}%"));
        }

        query = sortDescending
            ? query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            : query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id);

        return await query
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<Paper?> FindByIdAsync(long paperId, CancellationToken cancellationToken = default) =>
        db.Papers
            .AsNoTracking()
            .Include(x => x.Versions)
            .FirstOrDefaultAsync(x => x.Id == paperId && !x.Deleted, cancellationToken);

    public Task<bool> ExistsAsync(long paperId, CancellationToken cancellationToken = default) =>
        db.Papers.AnyAsync(x => x.Id == paperId && !x.Deleted, cancellationToken);
}
