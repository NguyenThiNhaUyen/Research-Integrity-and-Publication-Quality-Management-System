using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Application.Repositories.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Infrastructure.Configurations;

namespace PublicationQualitySystem.Infrastructure.Repositories.Implementations;

public class PaperReviewProfileRepository(ApplicationDbContext db) : IPaperReviewProfileRepository
{
    public Task<PaperReviewProfile?> FindByPaperIdAsync(long paperId) =>
        db.Set<PaperReviewProfile>()
            .FirstOrDefaultAsync(profile => profile.PaperId == paperId && !profile.Deleted);

    public async Task AddAsync(PaperReviewProfile profile) =>
        await db.Set<PaperReviewProfile>().AddAsync(profile);

    public Task SaveChangesAsync() =>
        db.SaveChangesAsync();
}
