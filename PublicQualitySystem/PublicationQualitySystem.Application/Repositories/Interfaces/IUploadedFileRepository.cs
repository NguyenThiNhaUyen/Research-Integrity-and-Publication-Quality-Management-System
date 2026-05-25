using PublicationQualitySystem.Domain.Entities;

namespace PublicationQualitySystem.Application.Repositories.Interfaces;

public interface IUploadedFileRepository
{
    Task<UploadedFile?> FindByIdAsync(long id);
    Task AddAsync(UploadedFile file);
    Task SaveChangesAsync();
}
