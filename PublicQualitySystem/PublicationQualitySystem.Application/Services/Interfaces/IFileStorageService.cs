using PublicationQualitySystem.Application.DTOs.File.Requests;
using PublicationQualitySystem.Application.DTOs.File.Responses;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadAsync(FileUploadRequestDto file, string folder);
    Task DeleteAsync(string fileKey);
    string GeneratePresignedDownloadUrl(string fileKey, DateTime expiresAt);
}
