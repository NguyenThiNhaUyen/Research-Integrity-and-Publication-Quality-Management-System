
namespace PublicationQualitySystem.Shared.Exceptions;

public static class AuthErrorCode
{
    public static readonly IErrorCode UserAlreadyExists = new ErrorCode("User already exists", 409);
    public static readonly IErrorCode UserNotConfirmed = new ErrorCode("User is not confirmed", 401);
    public static readonly IErrorCode InvalidCredentials = new ErrorCode("Invalid credentials", 401);
    public static readonly IErrorCode CognitoError = new ErrorCode("Cognito service error", 502);
    public static readonly IErrorCode TokenRefreshFailed = new ErrorCode("Token refresh failed", 401);
    public static readonly IErrorCode LogoutFailed = new ErrorCode("Logout failed", 400);
    public static readonly IErrorCode CognitoSubMissing = new ErrorCode("Cognito Sub is missing", 400);
}
