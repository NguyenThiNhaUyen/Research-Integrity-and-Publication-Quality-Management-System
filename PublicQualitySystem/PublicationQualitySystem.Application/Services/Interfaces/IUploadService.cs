using PublicationQualitySystem.Application.DTOs.File;
using PublicationQualitySystem.Application.DTOs.File.Requests;
using PublicationQualitySystem.Application.DTOs.File.Responses;
using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IUploadService
{
    Task<UploadedFileResponse> UploadAsync(FileUploadRequestDto file, UploadType type);
    Task<List<UploadedFileResponse>> GetFilesAsync();
    Task<UploadedFileResponse> GetFileAsync(long fileId);
}
