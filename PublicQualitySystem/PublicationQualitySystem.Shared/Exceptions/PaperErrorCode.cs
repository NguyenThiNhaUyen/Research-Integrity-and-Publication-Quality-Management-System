using PublicationQualitySystem.Shared.Common;
using System.Net;

namespace PublicationQualitySystem.Shared.Exceptions;

public static class PaperErrorCode
{
    public static readonly IErrorCode InvalidPdf = new ErrorCode("Only PDF files are supported.", HttpStatusCode.BadRequest);
    public static readonly IErrorCode EmptyFile = new ErrorCode("Uploaded file is empty.", HttpStatusCode.BadRequest);
    public static readonly IErrorCode StorageFailed = new ErrorCode("File storage failed.", HttpStatusCode.InternalServerError);
    public static readonly IErrorCode NotFound = new ErrorCode("Paper was not found.", HttpStatusCode.NotFound);
    public static readonly IErrorCode MetadataNotFound = new ErrorCode("Paper metadata was not found.", HttpStatusCode.NotFound);
}
