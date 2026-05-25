using PublicationQualitySystem.Domain.Entities;

namespace PublicationQualitySystem.Application.Repositories.Interfaces;

public interface IPaperVersionRepository
{
    Task<int> GetLatestVersionNumberAsync(long paperId);
    Task<PaperVersion?> FindByIdWithPaperAsync(long id);
    Task<PaperVersion?> FindByIdWithPaperIncludingDeletedAsync(long id);
    Task<PaperVersion?> FindByPaperAndIdAsync(long paperId, long id);
    Task<PaperVersion?> FindByPaperAndIdIncludingDeletedAsync(long paperId, long id);
    Task<PaperVersion?> FindByUploadedFileIdWithPaperAsync(long fileId);
    Task<List<PaperVersion>> FindByPaperIdAsync(long paperId);
    Task AddAsync(PaperVersion version);
    Task SaveChangesAsync();
}
