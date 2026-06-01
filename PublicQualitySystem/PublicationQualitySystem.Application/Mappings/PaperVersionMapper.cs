using PublicationQualitySystem.Application.DTOs.Paper;
using PublicationQualitySystem.Domain.Entities;

namespace PublicationQualitySystem.Application.Mappings;

public static class PaperVersionMapper
{
    public static PaperVersionResponseDto ToDto(PaperVersion version) => new()
    {
        Id = version.Id,
        PaperId = version.PaperId,
        VersionNumber = version.VersionNumber,
        VersionName = version.VersionName,
        ChangeLog = version.ChangeLog,
        FileId = version.UploadedFileId,
        OriginalFileName = version.UploadedFile?.OriginalFileName ?? version.OriginalFileName,
        FileKey = version.UploadedFile?.FileKey ?? version.FileKey,
        Url = version.UploadedFile?.Url ?? version.FileUrl,
        ContentType = version.UploadedFile?.ContentType ?? version.ContentType,
        Size = version.UploadedFile?.Size ?? version.Size,
        UploadedBy = version.UploadedBy ?? string.Empty,
        CreatedAt = version.CreatedAt,
        UpdatedAt = version.UpdatedAt,
        Deleted = version.Deleted
    };
}
