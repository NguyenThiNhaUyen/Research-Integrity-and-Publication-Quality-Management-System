using System.Net;


namespace PublicationQualitySystem.Shared.Exceptions;

public static class UserErrorCode
{
    public static readonly IErrorCode UserNotFound = new ErrorCode("User not found", HttpStatusCode.NotFound);
    public static readonly IErrorCode EmailAlreadyExists = new ErrorCode("Email already exists", HttpStatusCode.Conflict);
    public static readonly IErrorCode ValidationError = new ErrorCode("Validation error", HttpStatusCode.BadRequest);
}
