using PublicationQualitySystem.DTOs.File;

namespace PublicationQualitySystem.Services.Interfaces;

public interface IFileStorageService
{
    Task<S3FileResponseDto> UploadAsync(IFormFile file, string folder);
    Task DeleteAsync(string fileKey);
}
