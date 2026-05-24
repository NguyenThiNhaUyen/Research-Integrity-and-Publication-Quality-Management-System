using PublicationQualitySystem.Application.DTOs.File;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IFileStorageService
{
    Task<S3FileResponseDto> UploadAsync(FileUploadRequest file, string folder);
    Task DeleteAsync(string fileKey);
}
