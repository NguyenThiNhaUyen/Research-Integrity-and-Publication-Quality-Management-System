using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Application.Repositories.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Infrastructure.Configurations;

namespace PublicationQualitySystem.Infrastructure.Repositories.Implementations;

public class PaperVersionRepository(ApplicationDbContext db) : IPaperVersionRepository
{
    public async Task<IReadOnlyList<PaperVersion>> FindByPaperIdAsync(long paperId, CancellationToken cancellationToken = default) =>
        await db.PaperVersions
            .AsNoTracking()
            .Include(x => x.Paper)
            .Where(x => x.PaperId == paperId && !x.Deleted && !x.Paper.Deleted)
            .OrderByDescending(x => x.VersionNumber)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<PaperVersion?> FindByIdAsync(long paperVersionId, CancellationToken cancellationToken = default) =>
        db.PaperVersions
            .AsNoTracking()
            .Include(x => x.Paper)
            .FirstOrDefaultAsync(x => x.Id == paperVersionId && !x.Deleted && !x.Paper.Deleted, cancellationToken);
}
