using System.Net;

namespace PublicationQualitySystem.Shared.Exceptions;

public static class OcrErrorCode
{
    public static readonly IErrorCode ConfigurationMissing = new ErrorCode("OCR configuration is missing", HttpStatusCode.InternalServerError);
    public static readonly IErrorCode ObjectKeyRequired = new ErrorCode("S3 object key is required", HttpStatusCode.BadRequest);
    public static readonly IErrorCode JobStartFailed = new ErrorCode("Textract OCR job start failed", HttpStatusCode.BadGateway);
    public static readonly IErrorCode JobFailed = new ErrorCode("Textract OCR job failed", HttpStatusCode.BadGateway);
    public static readonly IErrorCode AwsServiceError = new ErrorCode("Textract AWS service error", HttpStatusCode.BadGateway);
    public static readonly IErrorCode S3ObjectNotFound = new ErrorCode("S3 object not found", HttpStatusCode.NotFound);
    public static readonly IErrorCode S3ObjectAccessDenied = new ErrorCode("S3 object access denied", HttpStatusCode.Forbidden);
    public static readonly IErrorCode S3ObjectWrongRegion = new ErrorCode("S3 object region mismatch", HttpStatusCode.BadGateway);
    public static readonly IErrorCode S3ObjectMetadataFailed = new ErrorCode("Unable to read S3 object metadata", HttpStatusCode.BadGateway);
    public static readonly IErrorCode AwsIdentityMismatch = new ErrorCode("Backend is using different AWS credentials.", HttpStatusCode.Forbidden);
    public static readonly IErrorCode AwsIdentityLookupFailed = new ErrorCode("Unable to resolve AWS caller identity", HttpStatusCode.BadGateway);
}
