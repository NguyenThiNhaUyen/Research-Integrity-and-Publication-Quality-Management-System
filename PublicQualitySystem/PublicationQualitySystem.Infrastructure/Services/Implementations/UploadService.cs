using Microsoft.Extensions.Options;
using PublicationQualitySystem.Application.DTOs.File;
using PublicationQualitySystem.Application.Repositories.Interfaces;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Options;
using PublicationQualitySystem.Infrastructure.Security;
using PublicationQualitySystem.Shared.Exceptions;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public class UploadService(
    IFileStorageService fileStorageService,
    IUploadedFileRepository uploadedFiles,
    ICurrentUserProvider currentUser,
    IOptions<ManuscriptUploadOptions> uploadOptions) : IUploadService
{
    private static readonly IReadOnlySet<string> PaperExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx"
    };

    private static readonly IReadOnlySet<string> PaperContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    private static readonly IReadOnlySet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private static readonly IReadOnlySet<string> ImageContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    private static readonly IReadOnlySet<string> DocumentExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx"
    };

    private static readonly IReadOnlySet<string> DocumentContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation"
    };

    private static readonly IReadOnlySet<string> OtherExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".webp", ".xls", ".xlsx", ".ppt", ".pptx"
    };

    public async Task<UploadedFileResponse> UploadAsync(FileUploadRequest file, UploadType type)
    {
        if (file is null || file.Length == 0)
        {
            throw new AppException(UploadFileErrorCode.FileIsEmpty);
        }

        if (file.Length > uploadOptions.Value.MaxFileSizeBytes)
        {
            throw new AppException(UploadFileErrorCode.FileTooLarge);
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !GetAllowedExtensions(type).Contains(extension))
        {
            throw new AppException(UploadFileErrorCode.InvalidFileType);
        }

        var allowedContentTypes = GetAllowedContentTypes(type);
        if (string.IsNullOrWhiteSpace(file.ContentType) ||
            allowedContentTypes is not null && !allowedContentTypes.Contains(file.ContentType))
        {
            throw new AppException(UploadFileErrorCode.InvalidFileType);
        }

        var fileKey = await fileStorageService.UploadAsync(file, GetFolder(type));
        var uploadedFile = new UploadedFile
        {
            OriginalFileName = file.FileName,
            FileName = Path.GetFileName(fileKey),
            FileKey = fileKey,
            Url = fileKey,
            ContentType = file.ContentType,
            Size = file.Length,
            UploadType = type,
            UploadedBy = currentUser.Subject
        };

        await uploadedFiles.AddAsync(uploadedFile);
        await uploadedFiles.SaveChangesAsync();

        return new UploadedFileResponse
        {
            FileId = uploadedFile.Id,
            FileName = uploadedFile.OriginalFileName,
            Url = uploadedFile.Url,
            Key = uploadedFile.FileKey,
            ContentType = uploadedFile.ContentType,
            Size = uploadedFile.Size
        };
    }

    private static IReadOnlySet<string> GetAllowedExtensions(UploadType type) => type switch
    {
        UploadType.PAPER => PaperExtensions,
        UploadType.PAPER_VERSION => PaperExtensions,
        UploadType.AVATAR => ImageExtensions,
        UploadType.DOCUMENT => DocumentExtensions,
        UploadType.TEMP => OtherExtensions,
        UploadType.OTHER => OtherExtensions,
        _ => OtherExtensions
    };

    private static IReadOnlySet<string>? GetAllowedContentTypes(UploadType type) => type switch
    {
        UploadType.PAPER => PaperContentTypes,
        UploadType.PAPER_VERSION => PaperContentTypes,
        UploadType.AVATAR => ImageContentTypes,
        UploadType.DOCUMENT => DocumentContentTypes,
        _ => null
    };

    private static string GetFolder(UploadType type) => type switch
    {
        UploadType.PAPER => "papers",
        UploadType.PAPER_VERSION => "paper-versions",
        UploadType.AVATAR => "avatars",
        UploadType.DOCUMENT => "documents",
        UploadType.TEMP => "temp",
        UploadType.OTHER => "others",
        _ => "others"
    };
}
