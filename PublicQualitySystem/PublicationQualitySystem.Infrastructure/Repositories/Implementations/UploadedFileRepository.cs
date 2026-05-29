using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Application.Repositories.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Infrastructure.Configurations;

namespace PublicationQualitySystem.Infrastructure.Repositories.Implementations;

public class UploadedFileRepository(ApplicationDbContext db) : IUploadedFileRepository
{
    public Task<UploadedFile?> FindByIdAsync(long id) =>
        db.UploadedFiles.FirstOrDefaultAsync(f => f.Id == id && !f.Deleted);

    public Task<List<UploadedFile>> GetAllAsync() =>
        db.UploadedFiles
            .Where(f => !f.Deleted)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(UploadedFile file) => await db.UploadedFiles.AddAsync(file);

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
