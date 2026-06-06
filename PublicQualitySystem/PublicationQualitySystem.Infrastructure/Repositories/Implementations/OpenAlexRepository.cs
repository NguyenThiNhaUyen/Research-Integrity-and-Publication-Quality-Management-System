using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Application.Repositories.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Infrastructure.Configurations;

namespace PublicationQualitySystem.Infrastructure.Repositories.Implementations;

public class OpenAlexRepository(ApplicationDbContext db) : IOpenAlexRepository
{
    public async Task<PaperSimilarityCheck?> FindSimilarityByPaperVersionIdAsync(long paperVersionId, CancellationToken cancellationToken = default)
    {
        var metadataId = await db.PaperVersions
            .AsNoTracking()
            .Where(x => x.Id == paperVersionId && !x.Deleted && !x.Paper.Deleted)
            .Join(
                db.PaperMetadata.AsNoTracking().Where(x => !x.Deleted),
                version => version.PaperId,
                metadata => metadata.PaperId,
                (_, metadata) => (long?)metadata.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (metadataId is null)
        {
            return null;
        }

        return await db.PaperSimilarityChecks
            .AsNoTracking()
            .Where(x => x.PaperMetadataId == metadataId.Value && !x.Deleted)
            .OrderByDescending(x => x.CheckedAt ?? x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
