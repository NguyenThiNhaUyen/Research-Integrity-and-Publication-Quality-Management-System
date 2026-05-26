using Amazon.Textract;
using Amazon.Textract.Model;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Infrastructure.Options;
using PublicationQualitySystem.Shared.Constants;
using PublicationQualitySystem.Shared.Exceptions;
using System.Text;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations
{
    public class TextractOcrService : IPdfOcrService
    {
        private readonly IAmazonTextract _textractClient;
        private readonly IAmazonS3 _s3Client;
        private readonly IAmazonSecurityTokenService _stsClient;
        private readonly S3Options _options;
        private readonly AwsOptions _awsOptions;
        private readonly ILogger<TextractOcrService> _logger;

        private string Bucket => _options.Bucket ?? string.Empty;
        private string Region => _awsOptions.Region ?? AwsConstants.DefaultRegion;

        public TextractOcrService(
            IAmazonTextract textractClient,
            IAmazonS3 s3Client,
            IAmazonSecurityTokenService stsClient,
            IOptions<S3Options> options,
            IOptions<AwsOptions> awsOptions,
            ILogger<TextractOcrService> logger)
        {
            _textractClient = textractClient;
            _s3Client = s3Client;
            _stsClient = stsClient;
            _options = options.Value;
            _awsOptions = awsOptions.Value;
            _logger = logger;
        }

        public async Task<string> ExtractTextAsync(string objectKey)
        {
            if (_textractClient is null)
            {
                throw new AppException(OcrErrorCode.ConfigurationMissing);
            }

            if (_s3Client is null)
            {
                throw new AppException(OcrErrorCode.ConfigurationMissing);
            }

            if (_stsClient is null)
            {
                throw new AppException(OcrErrorCode.ConfigurationMissing);
            }

            if (_options is null)
            {
                throw new AppException(OcrErrorCode.ConfigurationMissing);
            }

            if (string.IsNullOrWhiteSpace(Bucket))
            {
                throw new AppException(OcrErrorCode.ConfigurationMissing);
            }

            if (string.IsNullOrWhiteSpace(objectKey))
            {
                throw new AppException(OcrErrorCode.ObjectKeyRequired);
            }

            var normalizedKey = NormalizeObjectKey(objectKey);
            var identity = await GetCallerIdentityAsync(Bucket, normalizedKey);
            EnsureExpectedCaller(identity);

            var metadata = await GetS3MetadataAsync(Bucket, normalizedKey);
            var fileType = metadata.Headers.ContentType ?? Path.GetExtension(normalizedKey);
            var encryption = metadata.ServerSideEncryptionMethod?.ToString() ?? "None";

            _logger.LogInformation(
                "Starting Textract OCR. callerArn={CallerArn}, account={Account}, userId={UserId}, bucket={Bucket}, key={Key}, region={Region}, fileType={FileType}, sse={ServerSideEncryption}",
                identity.Arn,
                identity.Account,
                identity.UserId,
                Bucket,
                normalizedKey,
                Region,
                fileType,
                encryption);

            if (IsKmsEncrypted(metadata))
            {
                _logger.LogWarning(
                    "S3 object uses SSE-KMS. OCR caller may also need kms:Decrypt and kms:DescribeKey. callerArn={CallerArn}, bucket={Bucket}, key={Key}, kmsKeyId={KmsKeyId}",
                    identity.Arn,
                    Bucket,
                    normalizedKey,
                    metadata.ServerSideEncryptionKeyManagementServiceKeyId);
            }

            StartDocumentTextDetectionResponse startResponse;

            try
            {
                startResponse = await _textractClient.StartDocumentTextDetectionAsync(new StartDocumentTextDetectionRequest
                {
                    DocumentLocation = new DocumentLocation
                    {
                        S3Object = new Amazon.Textract.Model.S3Object
                        {
                            Bucket = Bucket,
                            Name = normalizedKey
                        }
                    }
                });
            }
            catch (AmazonServiceException exception)
            {
                throw new AppException(
                    OcrErrorCode.AwsServiceError,
                    $"Textract AWS service error [{exception.ErrorCode ?? "UNKNOWN"}]: {exception.Message}");
            }

            if (string.IsNullOrWhiteSpace(startResponse.JobId))
            {
                throw new AppException(OcrErrorCode.JobStartFailed);
            }

            var jobId = startResponse.JobId;
            GetDocumentTextDetectionResponse response;

            do
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
                try
                {
                    response = await _textractClient.GetDocumentTextDetectionAsync(new GetDocumentTextDetectionRequest
                    {
                        JobId = jobId
                    });
                }
                catch (AmazonServiceException exception)
                {
                    throw new AppException(
                        OcrErrorCode.AwsServiceError,
                        $"Textract AWS service error [{exception.ErrorCode ?? "UNKNOWN"}]: {exception.Message}");
                }

                if (response.JobStatus == JobStatus.FAILED)
                {
                    throw new AppException(OcrErrorCode.JobFailed);
                }
            }
            while (response.JobStatus == JobStatus.IN_PROGRESS);

            if (response.JobStatus != JobStatus.SUCCEEDED)
            {
                throw new AppException(OcrErrorCode.JobFailed);
            }

            var textBuilder = new StringBuilder();
            string? nextToken = null;

            do
            {
                try
                {
                    response = await _textractClient.GetDocumentTextDetectionAsync(new GetDocumentTextDetectionRequest
                    {
                        JobId = jobId,
                        NextToken = nextToken
                    });
                }
                catch (AmazonServiceException exception)
                {
                    throw new AppException(
                        OcrErrorCode.AwsServiceError,
                        $"Textract AWS service error [{exception.ErrorCode ?? "UNKNOWN"}]: {exception.Message}");
                }

                if (response.JobStatus == JobStatus.FAILED)
                {
                    throw new AppException(OcrErrorCode.JobFailed);
                }

                foreach (var block in response.Blocks ?? Enumerable.Empty<Block>())
                {
                    if (block.BlockType == BlockType.LINE && !string.IsNullOrWhiteSpace(block.Text))
                    {
                        textBuilder.AppendLine(block.Text);
                    }
                }

                nextToken = response.NextToken;
            }
            while (!string.IsNullOrWhiteSpace(nextToken));

            return textBuilder.ToString();
        }

        private async Task<GetCallerIdentityResponse> GetCallerIdentityAsync(string bucket, string key)
        {
            try
            {
                var identity = await _stsClient.GetCallerIdentityAsync(new GetCallerIdentityRequest());
                _logger.LogInformation(
                    "AWS caller identity resolved. callerArn={CallerArn}, account={Account}, userId={UserId}, region={Region}, bucket={Bucket}, key={Key}",
                    identity.Arn,
                    identity.Account,
                    identity.UserId,
                    Region,
                    bucket,
                    key);

                return identity;
            }
            catch (AmazonServiceException exception)
            {
                throw new AppException(
                    OcrErrorCode.AwsIdentityLookupFailed,
                    $"Unable to resolve AWS caller identity [{exception.ErrorCode ?? "UNKNOWN"}]: {exception.Message}. region={Region}, bucket={bucket}, key={key}");
            }
        }

        private void EnsureExpectedCaller(GetCallerIdentityResponse identity)
        {
            if (string.IsNullOrWhiteSpace(_awsOptions.ExpectedCallerArn))
            {
                _logger.LogWarning(
                    "AWS expected caller ARN is not configured. Set Aws:ExpectedCallerArn or AWS_EXPECTED_CALLER_ARN to verify backend credentials. actualCallerArn={CallerArn}, account={Account}, userId={UserId}",
                    identity.Arn,
                    identity.Account,
                    identity.UserId);
                return;
            }

            if (!string.Equals(identity.Arn, _awsOptions.ExpectedCallerArn, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(
                    "Backend is using different AWS credentials. expectedCallerArn={ExpectedCallerArn}, actualCallerArn={ActualCallerArn}, account={Account}, userId={UserId}",
                    _awsOptions.ExpectedCallerArn,
                    identity.Arn,
                    identity.Account,
                    identity.UserId);

                throw new AppException(OcrErrorCode.AwsIdentityMismatch);
            }
        }

        private async Task<GetObjectMetadataResponse> GetS3MetadataAsync(string bucket, string key)
        {
            try
            {
                return await _s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
                {
                    BucketName = bucket,
                    Key = key
                });
            }
            catch (AmazonS3Exception exception) when (IsNotFound(exception))
            {
                throw new AppException(OcrErrorCode.S3ObjectNotFound, $"S3 object not found. bucket={bucket}, key={key}");
            }
            catch (AmazonS3Exception exception) when (IsAccessDenied(exception))
            {
                _logger.LogWarning(
                    exception,
                    "S3 metadata access denied. Check IAM identity permissions, bucket policy explicit Deny, object ownership, and SSE-KMS key policy. region={Region}, bucket={Bucket}, key={Key}",
                    Region,
                    bucket,
                    key);

                throw new AppException(
                    OcrErrorCode.S3ObjectAccessDenied,
                    $"Permission denied reading S3 object metadata. Check bucket policy explicit Deny, object ownership, and SSE-KMS. Required permissions may include s3:GetObject, s3:GetObjectVersion, kms:Decrypt, kms:DescribeKey. region={Region}, bucket={bucket}, key={key}");
            }
            catch (AmazonS3Exception exception) when (IsWrongRegion(exception))
            {
                throw new AppException(OcrErrorCode.S3ObjectWrongRegion, $"Wrong S3 region for object metadata. configuredRegion={Region}, bucket={bucket}, key={key}");
            }
            catch (AmazonS3Exception exception)
            {
                throw new AppException(OcrErrorCode.S3ObjectMetadataFailed, $"Unable to read S3 object metadata [{exception.ErrorCode ?? "UNKNOWN"}]: {exception.Message}. bucket={bucket}, key={key}");
            }
        }

        private static string NormalizeObjectKey(string objectKey)
        {
            var key = objectKey.Trim();
            if (Uri.TryCreate(key, UriKind.Absolute, out var uri))
            {
                return Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
            }

            return key.TrimStart('/');
        }

        private static bool IsNotFound(AmazonS3Exception exception) =>
            exception.StatusCode == System.Net.HttpStatusCode.NotFound ||
            string.Equals(exception.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(exception.ErrorCode, "NotFound", StringComparison.OrdinalIgnoreCase);

        private static bool IsAccessDenied(AmazonS3Exception exception) =>
            exception.StatusCode == System.Net.HttpStatusCode.Forbidden ||
            string.Equals(exception.ErrorCode, "AccessDenied", StringComparison.OrdinalIgnoreCase);

        private static bool IsWrongRegion(AmazonS3Exception exception) =>
            string.Equals(exception.ErrorCode, "PermanentRedirect", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(exception.ErrorCode, "AuthorizationHeaderMalformed", StringComparison.OrdinalIgnoreCase);

        private static bool IsKmsEncrypted(GetObjectMetadataResponse metadata) =>
            metadata.ServerSideEncryptionMethod?.ToString().Contains("kms", StringComparison.OrdinalIgnoreCase) == true ||
            !string.IsNullOrWhiteSpace(metadata.ServerSideEncryptionKeyManagementServiceKeyId);
    }
}
