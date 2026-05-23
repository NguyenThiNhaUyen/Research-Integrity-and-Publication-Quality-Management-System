using PublicationQualitySystem.Exceptions;

namespace PublicationQualitySystem.Enums;

public sealed class ErrorCode(string message, int statusCode) : IErrorCode
{
    public int Code => StatusCode;
    public string Message { get; } = message;
    public int StatusCode { get; } = statusCode;
}
