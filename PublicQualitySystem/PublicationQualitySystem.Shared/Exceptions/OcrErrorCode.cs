namespace PublicationQualitySystem.Shared.Exceptions;

public static class OcrErrorCode
{
    public static readonly IErrorCode ConfigurationMissing = new ErrorCode("OCR configuration is missing", 500);
    public static readonly IErrorCode ObjectKeyRequired = new ErrorCode("S3 object key is required", 400);
    public static readonly IErrorCode JobStartFailed = new ErrorCode("Textract OCR job start failed", 502);
    public static readonly IErrorCode JobFailed = new ErrorCode("Textract OCR job failed", 502);
    public static readonly IErrorCode AwsServiceError = new ErrorCode("Textract AWS service error", 502);
    public static readonly IErrorCode S3ObjectNotFound = new ErrorCode("S3 object not found", 404);
    public static readonly IErrorCode S3ObjectAccessDenied = new ErrorCode("S3 object access denied", 403);
    public static readonly IErrorCode S3ObjectWrongRegion = new ErrorCode("S3 object region mismatch", 502);
    public static readonly IErrorCode S3ObjectMetadataFailed = new ErrorCode("Unable to read S3 object metadata", 502);
    public static readonly IErrorCode AwsIdentityMismatch = new ErrorCode("Backend is using different AWS credentials.", 403);
    public static readonly IErrorCode AwsIdentityLookupFailed = new ErrorCode("Unable to resolve AWS caller identity", 502);
}
