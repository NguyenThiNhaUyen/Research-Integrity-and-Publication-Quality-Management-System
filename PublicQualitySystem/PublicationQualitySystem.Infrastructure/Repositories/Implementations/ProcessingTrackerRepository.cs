using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Application.Repositories.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Infrastructure.Configurations;

namespace PublicationQualitySystem.Infrastructure.Repositories.Implementations;

public class ProcessingTrackerRepository(ApplicationDbContext db) : IProcessingTrackerRepository
{
    public Task<PaperProcessingTracker?> FindByPaperVersionIdAsync(long paperVersionId, CancellationToken cancellationToken = default) =>
        db.PaperProcessingTrackers
            .AsNoTracking()
            .Include(x => x.Events)
            .FirstOrDefaultAsync(x => x.PaperVersionId == paperVersionId, cancellationToken);
}
