using PublicationQualitySystem.Shared.Common;
using System.Net;

namespace PublicationQualitySystem.Shared.Exceptions;

public static class GrobidErrorCode
{
    public static readonly IErrorCode ExtractionFailed = new ErrorCode("GROBID metadata extraction failed.", HttpStatusCode.BadGateway);
    public static readonly IErrorCode InvalidResponse = new ErrorCode("GROBID service returned an invalid response.", HttpStatusCode.BadGateway);
    public static readonly IErrorCode ServiceUnavailable = new ErrorCode("GROBID service is unavailable.", HttpStatusCode.BadGateway);
}
