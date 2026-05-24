using PublicationQualitySystem.Application.DTOs.File;
using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IUploadService
{
    Task<S3FileResponseDto> UploadAsync(FileUploadRequest file, UploadType type);
}
