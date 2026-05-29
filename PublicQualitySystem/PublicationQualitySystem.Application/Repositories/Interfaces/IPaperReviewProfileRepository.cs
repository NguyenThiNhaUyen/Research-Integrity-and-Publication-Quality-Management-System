using PublicationQualitySystem.Domain.Entities;

namespace PublicationQualitySystem.Application.Repositories.Interfaces;

public interface IPaperReviewProfileRepository
{
    Task<PaperReviewProfile?> FindByPaperIdAsync(long paperId);
    Task AddAsync(PaperReviewProfile profile);
    Task SaveChangesAsync();
}
