using PublicationQualitySystem.DTOs.File;
using PublicationQualitySystem.Enums;
using PublicationQualitySystem.Exceptions;
using PublicationQualitySystem.Services.Interfaces;

namespace PublicationQualitySystem.Services.Implementations;

public class UploadService(IFileStorageService fileStorageService) : IUploadService
{
    private static readonly IReadOnlySet<string> ResearchPaperExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx"
    };

    private static readonly IReadOnlySet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private static readonly IReadOnlySet<string> EvidenceExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png"
    };

    private static readonly IReadOnlySet<string> OtherExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".webp"
    };

    public async Task<S3FileResponseDto> UploadAsync(IFormFile file, UploadType type)
    {
        if (file is null || file.Length == 0)
        {
            throw new AppException(UploadFileErrorCode.FileIsEmpty);
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !GetAllowedExtensions(type).Contains(extension))
        {
            throw new AppException(UploadFileErrorCode.InvalidFileType);
        }

        return await fileStorageService.UploadAsync(file, GetFolder(type));
    }

    private static IReadOnlySet<string> GetAllowedExtensions(UploadType type) => type switch
    {
        UploadType.ResearchPaper => ResearchPaperExtensions,
        UploadType.Avatar => ImageExtensions,
        UploadType.ResearchGroupLogo => ImageExtensions,
        UploadType.Evidence => EvidenceExtensions,
        UploadType.Other => OtherExtensions,
        _ => OtherExtensions
    };

    private static string GetFolder(UploadType type) => type switch
    {
        UploadType.ResearchPaper => "research-papers",
        UploadType.Avatar => "avatars",
        UploadType.ResearchGroupLogo => "research-group-logos",
        UploadType.Evidence => "evidences",
        UploadType.Other => "others",
        _ => "others"
    };
}
