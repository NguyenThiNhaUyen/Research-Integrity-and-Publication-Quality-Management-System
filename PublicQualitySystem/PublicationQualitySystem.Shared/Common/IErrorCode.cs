using System.Net;

namespace PublicationQualitySystem.Shared.Exceptions;

public interface IErrorCode
{
    string Message { get; }
    HttpStatusCode StatusCode { get; }
}
