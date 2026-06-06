using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Application.Repositories.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Infrastructure.Configurations;

namespace PublicationQualitySystem.Infrastructure.Repositories.Implementations;

public class MetadataQualityRepository(ApplicationDbContext db) : IMetadataQualityRepository
{
    public async Task<PaperMetadata?> FindByPaperVersionIdAsync(long paperVersionId, CancellationToken cancellationToken = default)
    {
        var paperId = await db.PaperVersions
            .AsNoTracking()
            .Where(x => x.Id == paperVersionId && !x.Deleted && !x.Paper.Deleted)
            .Select(x => (long?)x.PaperId)
            .FirstOrDefaultAsync(cancellationToken);

        if (paperId is null)
        {
            return null;
        }

        return await db.PaperMetadata
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PaperId == paperId.Value && !x.Deleted, cancellationToken);
    }
}
