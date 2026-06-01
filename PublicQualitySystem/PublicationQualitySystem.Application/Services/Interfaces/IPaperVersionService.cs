using PublicationQualitySystem.Application.DTOs.File;
using PublicationQualitySystem.Application.DTOs.Paper;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IPaperVersionService
{
    Task<PaperVersionResponseDto> CreateVersionAsync(long paperId, CreatePaperVersionRequest request);
    Task<List<PaperVersionResponseDto>> GetVersionsAsync(long paperId);
    Task<PaperVersionResponseDto> GetVersionAsync(long paperId, long versionId);
    Task<PaperVersionResponseDto> UpdateVersionAsync(long paperId, long versionId, UpdatePaperVersionRequest request);
    Task DeleteVersionAsync(long paperId, long versionId);
    Task<PaperVersionResponseDto> RestoreVersionAsync(long paperId, long versionId);
    Task<DownloadUrlResponseDto> GetDownloadUrlAsync(long fileId);
}
