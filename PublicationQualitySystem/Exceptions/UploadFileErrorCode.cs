using PublicationQualitySystem.Common;
using PublicationQualitySystem.Enums;

namespace PublicationQualitySystem.Exceptions;

public static class UploadFileErrorCode
{
    public static readonly IErrorCode UploadFileError = new ErrorCode("Upload file to S3 failed", StatusCodes.Status404NotFound);
    public static readonly IErrorCode DeleteFileFailed = new ErrorCode("Delete file from S3 failed", StatusCodes.Status400BadRequest);
    public static readonly IErrorCode FileIsEmpty = new ErrorCode("File is empty", StatusCodes.Status400BadRequest);
    public static readonly IErrorCode InvalidFileType = new ErrorCode("Invalid file type for upload type", StatusCodes.Status400BadRequest);
}
