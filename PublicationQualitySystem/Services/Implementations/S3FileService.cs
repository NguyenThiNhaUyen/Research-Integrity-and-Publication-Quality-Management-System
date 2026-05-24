using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using PublicationQualitySystem.DTOs.File;
using PublicationQualitySystem.Exceptions;
using PublicationQualitySystem.Options;
using PublicationQualitySystem.Services.Interfaces;

namespace PublicationQualitySystem.Services.Implementations;

public class S3FileService(IAmazonS3 s3, IOptions<S3Options> options) : IFileStorageService
{
    private string Bucket => options.Value.Bucket ?? string.Empty;

    public async Task<S3FileResponseDto> UploadAsync(IFormFile file, string folder)
    {
        try
        {
            var extension = Path.GetExtension(file.FileName) ?? string.Empty;
            var fileKey = $"{folder}/{Guid.NewGuid()}{extension}";
            await using var stream = file.OpenReadStream();
            await s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = Bucket,
                Key = fileKey,
                InputStream = stream,
                ContentType = file.ContentType
            });
            return new S3FileResponseDto { FileKey = fileKey };
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
}
