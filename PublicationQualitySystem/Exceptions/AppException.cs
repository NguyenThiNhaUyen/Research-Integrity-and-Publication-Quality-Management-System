namespace PublicationQualitySystem.Exceptions;

public class AppException : Exception
{
    public IErrorCode ErrorCode { get; }

    public AppException(IErrorCode errorCode) : base(errorCode.Message)
    {
        ErrorCode = errorCode;
    }

    public AppException(IErrorCode errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
