using PublicationQualitySystem.DTOs.File;
using PublicationQualitySystem.Enums;

namespace PublicationQualitySystem.Services.Interfaces;

public interface IUploadService
{
    Task<S3FileResponseDto> UploadAsync(IFormFile file, UploadType type);
}
