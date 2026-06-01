using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Application.Repositories.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Infrastructure.Configurations;

namespace PublicationQualitySystem.Infrastructure.Repositories.Implementations;

public class PaperVersionRepository(ApplicationDbContext db) : IPaperVersionRepository
{
    public async Task<int> GetLatestVersionNumberAsync(long paperId) =>
        await db.PaperVersions
            .Where(v => v.PaperId == paperId && !v.Deleted)
            .MaxAsync(v => (int?)v.VersionNumber) ?? 0;

    public Task<PaperVersion?> FindByIdWithPaperAsync(long id) =>
        db.PaperVersions
            .Include(v => v.UploadedFile)
            .Include(v => v.Paper)
            .ThenInclude(p => p.ResearchGroup)
            .ThenInclude(g => g!.Memberships)
            .FirstOrDefaultAsync(v => v.Id == id && !v.Deleted);

    public Task<PaperVersion?> FindByIdWithPaperIncludingDeletedAsync(long id) =>
        db.PaperVersions
            .Include(v => v.UploadedFile)
            .Include(v => v.Paper)
            .ThenInclude(p => p.ResearchGroup)
            .ThenInclude(g => g!.Memberships)
            .FirstOrDefaultAsync(v => v.Id == id);

    public Task<PaperVersion?> FindByPaperAndIdAsync(long paperId, long id) =>
        db.PaperVersions
            .Include(v => v.UploadedFile)
            .Include(v => v.Paper)
            .ThenInclude(p => p.ResearchGroup)
            .ThenInclude(g => g!.Memberships)
            .FirstOrDefaultAsync(v => v.PaperId == paperId && v.Id == id && !v.Deleted);

    public Task<PaperVersion?> FindByPaperAndIdIncludingDeletedAsync(long paperId, long id) =>
        db.PaperVersions
            .Include(v => v.UploadedFile)
            .Include(v => v.Paper)
            .ThenInclude(p => p.ResearchGroup)
            .ThenInclude(g => g!.Memberships)
            .FirstOrDefaultAsync(v => v.PaperId == paperId && v.Id == id);

    public Task<PaperVersion?> FindByUploadedFileIdWithPaperAsync(long fileId) =>
        db.PaperVersions
            .Include(v => v.UploadedFile)
            .Include(v => v.Paper)
            .ThenInclude(p => p.ResearchGroup)
            .ThenInclude(g => g!.Memberships)
            .FirstOrDefaultAsync(v => v.UploadedFileId == fileId && !v.Deleted);

    public Task<List<PaperVersion>> FindByPaperIdAsync(long paperId) =>
        db.PaperVersions
            .Include(v => v.UploadedFile)
            .Where(v => v.PaperId == paperId && !v.Deleted)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync();

    public async Task AddAsync(PaperVersion version) => await db.PaperVersions.AddAsync(version);

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
