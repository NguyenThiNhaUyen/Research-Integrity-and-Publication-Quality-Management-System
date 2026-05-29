using System.Net;

namespace PublicationQualitySystem.Shared.Exceptions;

public sealed class ErrorCode : IErrorCode
{
    public string Message { get; }

    public HttpStatusCode StatusCode { get; }

    public ErrorCode(string message, HttpStatusCode statusCode)
    {
        Message = message;
        StatusCode = statusCode;
    }
}
