using System.Diagnostics;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Infrastructure.Options;
using PublicationQualitySystem.Shared.Exceptions;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public class S3FileStorageService(
    IAmazonS3 s3,
    IOptions<S3Options> options,
    ILogger<S3FileStorageService> logger) : IFileStorageService
{
    public async Task<string> UploadAsync(
        Stream content,
        string key,
        string contentType,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var bucket = options.Value.Bucket;
        if (string.IsNullOrWhiteSpace(bucket))
        {
            logger.LogError("S3 upload failed because bucket is not configured. Key={Key}, ContentType={ContentType}", key, contentType);
            throw new AppException(PaperErrorCode.StorageFailed, "AWS S3 bucket is not configured.");
        }

        logger.LogInformation(
            "S3 upload started. Bucket={Bucket}, Key={Key}, ContentType={ContentType}, CanSeek={CanSeek}, Length={Length}",
            bucket,
            key,
            contentType,
            content.CanSeek,
            content.CanSeek ? content.Length : (long?)null);

        try
        {
            await s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucket,
                Key = key,
                InputStream = content,
                ContentType = contentType
            }, cancellationToken);

            logger.LogInformation(
                "S3 upload completed. Bucket={Bucket}, Key={Key}, ContentType={ContentType}, ElapsedMs={ElapsedMs}",
                bucket,
                key,
                contentType,
                stopwatch.ElapsedMilliseconds);
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogError(
                ex,
                "S3 upload failed. Bucket={Bucket}, Key={Key}, ContentType={ContentType}, StatusCode={StatusCode}, ErrorCode={ErrorCode}, ElapsedMs={ElapsedMs}",
                bucket,
                key,
                contentType,
                ex.StatusCode,
                ex.ErrorCode,
                stopwatch.ElapsedMilliseconds);
            throw new AppException(PaperErrorCode.StorageFailed, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "S3 upload failed unexpectedly. Bucket={Bucket}, Key={Key}, ContentType={ContentType}, ElapsedMs={ElapsedMs}",
                bucket,
                key,
                contentType,
                stopwatch.ElapsedMilliseconds);
            throw;
        }

        return key;
    }

    public async Task<Stream> DownloadAsync(
        string key,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var bucket = options.Value.Bucket;
        if (string.IsNullOrWhiteSpace(bucket))
        {
            logger.LogError("S3 download failed because bucket is not configured. Key={Key}", key);
            throw new AppException(PaperErrorCode.StorageFailed, "AWS S3 bucket is not configured.");
        }

        logger.LogInformation("S3 download started. Bucket={Bucket}, Key={Key}", bucket, key);

        try
        {
            using var response = await s3.GetObjectAsync(new GetObjectRequest
            {
                BucketName = bucket,
                Key = key
            }, cancellationToken);

            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            logger.LogInformation(
                "S3 download completed. Bucket={Bucket}, Key={Key}, Bytes={Bytes}, ElapsedMs={ElapsedMs}",
                bucket,
                key,
                memoryStream.Length,
                stopwatch.ElapsedMilliseconds);

            return memoryStream;
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogError(
                ex,
                "S3 download failed. Bucket={Bucket}, Key={Key}, StatusCode={StatusCode}, ErrorCode={ErrorCode}, ElapsedMs={ElapsedMs}",
                bucket,
                key,
                ex.StatusCode,
                ex.ErrorCode,
                stopwatch.ElapsedMilliseconds);
            throw new AppException(PaperErrorCode.StorageFailed, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "S3 download failed unexpectedly. Bucket={Bucket}, Key={Key}, ElapsedMs={ElapsedMs}",
                bucket,
                key,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
