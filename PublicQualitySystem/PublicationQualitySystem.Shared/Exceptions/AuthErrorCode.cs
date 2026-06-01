using System.Net;


namespace PublicationQualitySystem.Shared.Exceptions;

public static class AuthErrorCode
{
    public static readonly IErrorCode UserAlreadyExists = new ErrorCode("User already exists", HttpStatusCode.Conflict);
    public static readonly IErrorCode UserNotConfirmed = new ErrorCode("User is not confirmed", HttpStatusCode.Unauthorized);
    public static readonly IErrorCode InvalidCredentials = new ErrorCode("Invalid credentials", HttpStatusCode.Unauthorized);
    public static readonly IErrorCode CognitoError = new ErrorCode("Cognito service error", HttpStatusCode.BadGateway);
    public static readonly IErrorCode TokenRefreshFailed = new ErrorCode("Token refresh failed", HttpStatusCode.Unauthorized);
    public static readonly IErrorCode LogoutFailed = new ErrorCode("Logout failed", HttpStatusCode.BadRequest);
    public static readonly IErrorCode CognitoSubMissing = new ErrorCode("Cognito Sub is missing", HttpStatusCode.BadRequest);
}
