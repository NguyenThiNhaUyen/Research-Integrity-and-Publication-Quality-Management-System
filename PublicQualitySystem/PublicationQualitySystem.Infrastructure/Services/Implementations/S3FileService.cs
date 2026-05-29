using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using PublicationQualitySystem.Application.DTOs.File;
using PublicationQualitySystem.Shared.Exceptions;
using PublicationQualitySystem.Infrastructure.Options;
using PublicationQualitySystem.Application.Services.Interfaces;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public class S3FileService(IAmazonS3 s3, IOptions<S3Options> options) : IFileStorageService
{
    private string Bucket => options.Value.Bucket ?? string.Empty;

    public async Task<string> UploadAsync(FileUploadRequest file, string folder)
    {
        try
        {
            var extension = Path.GetExtension(file.FileName) ?? string.Empty;
            var fileKey = $"{folder}/{Guid.NewGuid()}{extension}";
            await s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = Bucket,
                Key = fileKey,
                InputStream = file.Content,
                ContentType = file.ContentType
            });
            return fileKey;
        }
        catch
        {
            throw new AppException(UploadFileErrorCode.UploadFileError);
        }
    }

    public async Task DeleteAsync(string fileKey)
    {
        try
        {
            await s3.DeleteObjectAsync(new DeleteObjectRequest { BucketName = Bucket, Key = fileKey });
        }
        catch
        {
            throw new AppException(UploadFileErrorCode.DeleteFileFailed);
        }
    }

    public string GeneratePresignedDownloadUrl(string fileKey, DateTime expiresAt)
    {
        try
        {
            return s3.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = Bucket,
                Key = fileKey,
                Expires = expiresAt,
                Verb = HttpVerb.GET
            });
        }
        catch
        {
            throw new AppException(UploadFileErrorCode.GenerateDownloadUrlFailed);
        }
    }
}
