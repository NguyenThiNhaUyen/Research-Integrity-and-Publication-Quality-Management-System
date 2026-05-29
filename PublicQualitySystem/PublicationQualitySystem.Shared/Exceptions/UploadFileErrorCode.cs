using System.Net;


namespace PublicationQualitySystem.Shared.Exceptions;

public static class UploadFileErrorCode
{
    public static readonly IErrorCode UploadFileError = new ErrorCode("Upload file to S3 failed", HttpStatusCode.NotFound);
    public static readonly IErrorCode DeleteFileFailed = new ErrorCode("Delete file from S3 failed", HttpStatusCode.BadRequest);
    public static readonly IErrorCode GenerateDownloadUrlFailed = new ErrorCode("Generate download URL failed", HttpStatusCode.BadRequest);
    public static readonly IErrorCode FileIsEmpty = new ErrorCode("File is empty", HttpStatusCode.BadRequest);
    public static readonly IErrorCode InvalidFileType = new ErrorCode("Invalid file type for upload type", HttpStatusCode.BadRequest);
    public static readonly IErrorCode FileTooLarge = new ErrorCode("File exceeds max upload size", HttpStatusCode.BadRequest);
    public static readonly IErrorCode FileNotFound = new ErrorCode("File not found", HttpStatusCode.NotFound);
    public static readonly IErrorCode PaperNotFound = new ErrorCode("Paper not found", HttpStatusCode.NotFound);
    public static readonly IErrorCode UploadForbidden = new ErrorCode("You are not allowed to access this paper file", HttpStatusCode.Forbidden);
}
