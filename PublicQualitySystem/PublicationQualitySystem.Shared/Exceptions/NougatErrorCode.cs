using PublicationQualitySystem.Shared.Common;
using System.Net;

namespace PublicationQualitySystem.Shared.Exceptions;

public static class NougatErrorCode
{
    public static readonly IErrorCode ConversionFailed = new ErrorCode("Nougat conversion failed.", HttpStatusCode.BadGateway);
    public static readonly IErrorCode InvalidResponse = new ErrorCode("Nougat service returned an invalid response.", HttpStatusCode.BadGateway);
    public static readonly IErrorCode ServiceUnavailable = new ErrorCode("Nougat service is unavailable.", HttpStatusCode.BadGateway);
}
