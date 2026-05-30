using Microsoft.Extensions.Options;
using PublicationQualitySystem.Application.DTOs.File;
using PublicationQualitySystem.Application.DTOs.Paper;
using PublicationQualitySystem.Application.Repositories.Interfaces;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Configurations;
using PublicationQualitySystem.Infrastructure.Options;
using PublicationQualitySystem.Infrastructure.Security;
using PublicationQualitySystem.Shared.Constants;
using PublicationQualitySystem.Shared.Exceptions;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public class UploadService(
    IFileStorageService fileStorageService,
    IUploadedFileRepository uploadedFiles,
    ICurrentUserProvider currentUser,
    IPaperUploadProcessor paperUploadProcessor,
    ApplicationDbContext db,
    IOptions<S3Options> s3Options,
    IOptions<AwsOptions> awsOptions,
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
        var bucket = s3Options.Value.Bucket ?? string.Empty;
        var region = awsOptions.Value.Region ?? AwsConstants.DefaultRegion;
        var fileUrl = BuildS3Url(bucket, region, fileKey);

        var uploadedFile = new UploadedFile
        {
            OriginalFileName = file.FileName,
            FileName = Path.GetFileName(fileKey),
            FileKey = fileKey,
            S3Bucket = bucket,
            S3Key = fileKey,
            Url = fileUrl,
            ContentType = file.ContentType,
            Size = file.Length,
            UploadType = type,
            UploadedBy = currentUser.Subject
        };

        if (type != UploadType.PAPER)
        {
            await uploadedFiles.AddAsync(uploadedFile);
            await uploadedFiles.SaveChangesAsync();
            return ToResponse(uploadedFile);
        }

        await using var transaction = await db.Database.BeginTransactionAsync();
        await uploadedFiles.AddAsync(uploadedFile);
        await uploadedFiles.SaveChangesAsync();

        var paperResult = await paperUploadProcessor.CreatePaperFromUploadedFileAsync(uploadedFile, file);
        await transaction.CommitAsync();

        return ToResponse(uploadedFile, paperResult);
    }

    public async Task<List<UploadedFileResponse>> GetFilesAsync() =>
        (await uploadedFiles.GetAllAsync())
            .Select(file => ToResponse(file))
            .ToList();

    public async Task<UploadedFileResponse> GetFileAsync(long fileId)
    {
        var file = await uploadedFiles.FindByIdAsync(fileId)
            ?? throw new AppException(UploadFileErrorCode.FileNotFound);

        return ToResponse(file);
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

    private static string BuildS3Url(string bucket, string region, string key)
    {
        if (string.IsNullOrWhiteSpace(bucket))
        {
            return key;
        }

        var escapedKey = string.Join("/", key.Split('/').Select(Uri.EscapeDataString));
        return region.Equals("us-east-1", StringComparison.OrdinalIgnoreCase)
            ? $"https://{bucket}.s3.amazonaws.com/{escapedKey}"
            : $"https://{bucket}.s3.{region}.amazonaws.com/{escapedKey}";
    }

    private static UploadedFileResponse ToResponse(
        UploadedFile file,
        PaperUploadProcessingResult? paper = null) => new()
    {
        FileId = file.Id,
        FileName = file.OriginalFileName,
        Url = file.Url,
        Key = file.FileKey,
        S3Bucket = file.S3Bucket,
        S3Key = string.IsNullOrWhiteSpace(file.S3Key) ? file.FileKey : file.S3Key,
        ContentType = file.ContentType,
        FileType = Path.GetExtension(file.OriginalFileName),
        Size = file.Size,
        PaperId = paper?.PaperId,
        PaperCode = paper?.PaperCode,
        Title = paper?.Title,
        AbstractText = paper?.AbstractText,
        Keywords = paper?.Keywords,
        ResearchField = paper?.ResearchField,
        CurrentVersion = paper?.CurrentVersion,
        SubmissionStatus = paper?.SubmissionStatus
    };
}
