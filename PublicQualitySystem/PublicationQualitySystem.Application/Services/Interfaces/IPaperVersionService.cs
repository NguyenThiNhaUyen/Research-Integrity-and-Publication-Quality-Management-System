using PublicationQualitySystem.Application.DTOs.File;
using PublicationQualitySystem.Application.DTOs.Paper;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IPaperVersionService
{
    Task<PaperVersionResponse> CreateVersionAsync(long paperId, CreatePaperVersionRequest request);
    Task<List<PaperVersionResponse>> GetVersionsAsync(long paperId);
    Task<PaperVersionResponse> GetVersionAsync(long paperId, long versionId);
    Task<PaperVersionResponse> UpdateVersionAsync(long paperId, long versionId, UpdatePaperVersionRequest request);
    Task DeleteVersionAsync(long paperId, long versionId);
    Task<PaperVersionResponse> RestoreVersionAsync(long paperId, long versionId);
    Task<DownloadUrlResponse> GetDownloadUrlAsync(long fileId);
}
