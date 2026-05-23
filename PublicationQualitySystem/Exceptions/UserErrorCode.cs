using PublicationQualitySystem.Enums;

namespace PublicationQualitySystem.Exceptions;

public static class UserErrorCode
{
    public static readonly IErrorCode UserNotFound = new ErrorCode("User not found", StatusCodes.Status404NotFound);
    public static readonly IErrorCode EmailAlreadyExists = new ErrorCode("Email already exists", StatusCodes.Status409Conflict);
    public static readonly IErrorCode ValidationError = new ErrorCode("Validation error", StatusCodes.Status400BadRequest);
}
