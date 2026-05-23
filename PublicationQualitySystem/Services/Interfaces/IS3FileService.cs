using PublicationQualitySystem.DTOs.Auth;
using PublicationQualitySystem.DTOs.File;
using PublicationQualitySystem.DTOs.ResearchGroup;
using PublicationQualitySystem.DTOs.ResearchProfile;
using PublicationQualitySystem.DTOs.Role;
using PublicationQualitySystem.DTOs.User;

namespace PublicationQualitySystem.Services.Interfaces;

public interface IS3FileService
{
    Task<S3FileResponseDto> UploadFileAsync(IFormFile file);
    Task DeleteFileAsync(string fileKey);
}
