using Amazon.S3;
using Amazon.S3.Model;
using PublicationQualitySystem.DTOs.Auth;
using PublicationQualitySystem.DTOs.File;
using PublicationQualitySystem.DTOs.ResearchGroup;
using PublicationQualitySystem.DTOs.ResearchProfile;
using PublicationQualitySystem.DTOs.Role;
using PublicationQualitySystem.DTOs.User;
using PublicationQualitySystem.Exceptions;
using PublicationQualitySystem.Services.Interfaces;

namespace PublicationQualitySystem.Services.Implementations;

public class S3FileService(IAmazonS3 s3, IConfiguration config) : IS3FileService
{
    private string Bucket => config["Aws:S3:Bucket"] ?? string.Empty;

    public async Task<S3FileResponseDto> UploadFileAsync(IFormFile file)
    {
        try
        {
            if (file.Length == 0) throw new InvalidOperationException("File is empty");
            var extension = Path.GetExtension(file.FileName) ?? string.Empty;
            var fileKey = $"uploads/{Guid.NewGuid()}{extension}";
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

    public async Task DeleteFileAsync(string fileKey)
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
