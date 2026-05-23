using PublicationQualitySystem.DTOs;

namespace PublicationQualitySystem.Services;

public interface IS3FileService
{
    Task<S3FileResponseDto> UploadFileAsync(IFormFile file);
    Task DeleteFileAsync(string fileKey);
}
