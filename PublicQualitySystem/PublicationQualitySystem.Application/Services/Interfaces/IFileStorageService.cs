using PublicationQualitySystem.Application.DTOs.File.Requests;
using PublicationQualitySystem.Application.DTOs.File.Responses;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IFileStorageService
{
    Task<S3FileResponseDto> UploadAsync(FileUploadRequestDto file, string folder);
    Task DeleteAsync(string fileKey);
}
