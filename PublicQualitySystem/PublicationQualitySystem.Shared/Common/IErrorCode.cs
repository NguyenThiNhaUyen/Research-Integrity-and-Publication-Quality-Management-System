namespace PublicationQualitySystem.Shared.Exceptions;

public interface IErrorCode
{
    int Code { get; }
    string Message { get; }
    int StatusCode { get; }
}
