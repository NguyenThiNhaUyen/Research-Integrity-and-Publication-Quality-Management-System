using PublicationQualitySystem.Enums;

namespace PublicationQualitySystem.Exceptions;

public static class UploadFileErrorCode
{
    public static readonly IErrorCode UploadFileError = new ErrorCode("Upload file to S3 failed", StatusCodes.Status404NotFound);
    public static readonly IErrorCode DeleteFileFailed = new ErrorCode("Delete file from S3 failed", StatusCodes.Status400BadRequest);
}
