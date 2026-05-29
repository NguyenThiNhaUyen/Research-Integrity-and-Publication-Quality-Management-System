using PublicationQualitySystem.Application.DTOs.File.Requests;
using PublicationQualitySystem.Application.DTOs.File.Responses;
using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IUploadService
{
    Task<S3FileResponseDto> UploadAsync(FileUploadRequestDto file, UploadType type);
}
