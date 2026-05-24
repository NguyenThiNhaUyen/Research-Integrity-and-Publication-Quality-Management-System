
namespace PublicationQualitySystem.Shared.Exceptions;

public static class UploadFileErrorCode
{
    public static readonly IErrorCode UploadFileError = new ErrorCode("Upload file to S3 failed", 404);
    public static readonly IErrorCode DeleteFileFailed = new ErrorCode("Delete file from S3 failed", 400);
    public static readonly IErrorCode FileIsEmpty = new ErrorCode("File is empty", 400);
    public static readonly IErrorCode InvalidFileType = new ErrorCode("Invalid file type for upload type", 400);
}
