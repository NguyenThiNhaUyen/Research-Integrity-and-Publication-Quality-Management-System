using System.Security.Claims;
using Microsoft.Extensions.Options;
using PublicationQualitySystem.Application.DTOs.File;
using PublicationQualitySystem.Application.DTOs.Paper;
using PublicationQualitySystem.Application.Mappings;
using PublicationQualitySystem.Application.Repositories.Interfaces;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Options;
using PublicationQualitySystem.Infrastructure.Security;
using PublicationQualitySystem.Shared.Constants;
using PublicationQualitySystem.Shared.Exceptions;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public class PaperVersionService(
    IPaperRepository papers,
    IPaperVersionRepository versions,
    IUploadedFileRepository uploadedFiles,
    IFileStorageService fileStorageService,
    ICurrentUserProvider currentUser,
    IOptions<ManuscriptUploadOptions> uploadOptions) : IPaperVersionService
{
    public async Task<PaperVersionResponse> CreateVersionAsync(CreatePaperVersionRequest request)
    {
        var paper = await GetAuthorizedPaperAsync(paperId);
        var uploadedFile = await uploadedFiles.FindByIdAsync(request.FileId)
            ?? throw new AppException(UploadFileErrorCode.FileNotFound);

        if (uploadedFile.UploadType is not (UploadType.PAPER_VERSION or UploadType.PAPER))
        {
            throw new AppException(UploadFileErrorCode.InvalidFileType);
        }

        if (!IsAdmin() && uploadedFile.UploadedBy != GetCurrentUserId())
        {
            throw new AppException(UploadFileErrorCode.UploadForbidden);
        }

        var versionNumber = await versions.GetLatestVersionNumberAsync(paperId) + 1;
        var version = new PaperVersion
        {
            PaperId = paper.Id,
            Paper = paper,
            UploadedFileId = uploadedFile.Id,
            UploadedFile = uploadedFile,
            VersionNumber = versionNumber,
            VersionName = request.VersionName,
            ChangeLog = request.ChangeLog,
            OriginalFileName = uploadedFile.OriginalFileName,
            FileKey = uploadedFile.FileKey,
            FileUrl = uploadedFile.Url,
            ContentType = uploadedFile.ContentType,
            FileType = Path.GetExtension(uploadedFile.OriginalFileName),
            Size = uploadedFile.Size,
            UploadedBy = GetCurrentUserId()
        };

        paper.CurrentVersion = versionNumber;
        paper.FileUrl = uploadedFile.Url;
        paper.FileType = version.FileType;
        paper.S3Bucket = uploadedFile.S3Bucket;
        paper.S3Key = string.IsNullOrWhiteSpace(uploadedFile.S3Key) ? uploadedFile.FileKey : uploadedFile.S3Key;

        await versions.AddAsync(version);
        await versions.SaveChangesAsync();
        return PaperVersionMapper.ToResponse(version);
    }

    public async Task<List<PaperVersionResponse>> GetVersionsAsync(long paperId)
    {
        await GetAuthorizedPaperAsync(paperId);
        return (await versions.FindByPaperIdAsync(paperId))
            .Select(PaperVersionMapper.ToResponse)
            .ToList();
    }

    public async Task<PaperVersionResponse> GetVersionAsync(long paperId, long versionId)
    {
        var version = await versions.FindByPaperAndIdAsync(paperId, versionId)
            ?? throw new AppException(UploadFileErrorCode.FileNotFound);

        EnsureCanAccess(version.Paper);
        return PaperVersionMapper.ToResponse(version);
    }

    public async Task<PaperVersionResponse> UpdateVersionAsync(long paperId, long versionId, UpdatePaperVersionRequest request)
    {
        var version = await versions.FindByPaperAndIdAsync(paperId, versionId)
            ?? throw new AppException(UploadFileErrorCode.FileNotFound);

        EnsureCanAccess(version.Paper);
        version.VersionName = request.VersionName;
        version.ChangeLog = request.ChangeLog;
        await versions.SaveChangesAsync();
        return PaperVersionMapper.ToResponse(version);
    }

    public async Task DeleteVersionAsync(long paperId, long versionId)
    {
        var version = await versions.FindByPaperAndIdAsync(paperId, versionId)
            ?? throw new AppException(UploadFileErrorCode.FileNotFound);

        EnsureCanAccess(version.Paper);
        version.Deleted = true;
        await versions.SaveChangesAsync();
    }

    public async Task<PaperVersionResponse> RestoreVersionAsync(long paperId, long versionId)
    {
        var version = await versions.FindByPaperAndIdIncludingDeletedAsync(paperId, versionId)
            ?? throw new AppException(UploadFileErrorCode.FileNotFound);

        EnsureCanAccess(version.Paper);
        version.Deleted = false;
        await versions.SaveChangesAsync();
        return PaperVersionMapper.ToResponse(version);
    }

    public async Task<DownloadUrlResponse> GetDownloadUrlAsync(long fileId)
    {
        var file = await uploadedFiles.FindByIdAsync(fileId)
            ?? throw new AppException(UploadFileErrorCode.FileNotFound);

        var version = await versions.FindByUploadedFileIdWithPaperAsync(fileId);
        if (version is not null)
        {
            EnsureCanAccess(version.Paper);
        }
        else if (!IsAdmin() && file.UploadedBy != GetCurrentUserId())
        {
            throw new AppException(UploadFileErrorCode.UploadForbidden);
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(Math.Max(1, uploadOptions.Value.DownloadUrlExpirationMinutes));
        return new DownloadUrlResponse
        {
            FileId = file.Id,
            FileKey = file.FileKey,
            Url = fileStorageService.GeneratePresignedDownloadUrl(file.FileKey, expiresAt),
            ExpiresAt = expiresAt
        };
    }

    private async Task<Paper> GetAuthorizedPaperAsync(long paperId)
    {
        var paper = await papers.FindByIdWithAccessDataAsync(paperId)
            ?? throw new AppException(UploadFileErrorCode.PaperNotFound);
        EnsureCanAccess(paper);
        return paper;
    }

    private void EnsureCanAccess(Paper paper)
    {
        if (IsAdmin()) return;

        var userId = GetCurrentUserId();
        if (!string.IsNullOrWhiteSpace(paper.OwnerUserId) && paper.OwnerUserId == userId) return;

        if (paper.ResearchGroupId.HasValue && paper.ResearchGroup is not null &&
            paper.ResearchGroup.Memberships.Any(m => m.UserId == userId && m.Status == MemberStatus.ACTIVE))
        {
            return;
        }

        throw new AppException(UploadFileErrorCode.UploadForbidden);
    }

    private string GetCurrentUserId() =>
        currentUser.Subject ?? throw new AppException(UploadFileErrorCode.UploadForbidden);

    private bool IsAdmin() =>
        currentUser.User.HasClaim(ClaimConstants.Role, $"{ClaimConstants.RolePrefix}{RoleName.ADMIN}") ||
        currentUser.User.HasClaim(ClaimConstants.Permission, PermissionName.ADMIN_SETTING_MANAGE.ToString());
}
