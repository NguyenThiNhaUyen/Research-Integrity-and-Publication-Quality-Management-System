
namespace PublicationQualitySystem.Shared.Exceptions;

public static class UploadFileErrorCode
{
    public static readonly IErrorCode UploadFileError = new ErrorCode("Upload file to S3 failed", 404);
    public static readonly IErrorCode DeleteFileFailed = new ErrorCode("Delete file from S3 failed", 400);
    public static readonly IErrorCode GenerateDownloadUrlFailed = new ErrorCode("Generate download URL failed", 400);
    public static readonly IErrorCode FileIsEmpty = new ErrorCode("File is empty", 400);
    public static readonly IErrorCode InvalidFileType = new ErrorCode("Invalid file type for upload type", 400);
    public static readonly IErrorCode FileTooLarge = new ErrorCode("File exceeds max upload size", 400);
    public static readonly IErrorCode FileNotFound = new ErrorCode("File not found", 404);
    public static readonly IErrorCode PaperNotFound = new ErrorCode("Paper not found", 404);
    public static readonly IErrorCode UploadForbidden = new ErrorCode("You are not allowed to access this paper file", 403);
}
