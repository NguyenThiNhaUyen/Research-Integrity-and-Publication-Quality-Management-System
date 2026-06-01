
namespace PublicationQualitySystem.Shared.Exceptions;

public static class UserErrorCode
{
    public static readonly IErrorCode UserNotFound = new ErrorCode("User not found", 404);
    public static readonly IErrorCode EmailAlreadyExists = new ErrorCode("Email already exists", 409);
    public static readonly IErrorCode ValidationError = new ErrorCode("Validation error", 400);
}
