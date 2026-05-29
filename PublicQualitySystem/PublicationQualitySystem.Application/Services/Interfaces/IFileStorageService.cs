using PublicationQualitySystem.Application.DTOs.File;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadAsync(FileUploadRequest file, string folder);
    Task DeleteAsync(string fileKey);
    string GeneratePresignedDownloadUrl(string fileKey, DateTime expiresAt);
}
