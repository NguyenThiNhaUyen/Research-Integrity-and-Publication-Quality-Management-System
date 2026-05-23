namespace PublicationQualitySystem.Exceptions;

public static class AuthErrorCode
{
    public static readonly IErrorCode UserAlreadyExists = new ErrorCode("User already exists", StatusCodes.Status409Conflict);
    public static readonly IErrorCode UserNotConfirmed = new ErrorCode("User is not confirmed", StatusCodes.Status401Unauthorized);
    public static readonly IErrorCode InvalidCredentials = new ErrorCode("Invalid credentials", StatusCodes.Status401Unauthorized);
    public static readonly IErrorCode CognitoError = new ErrorCode("Cognito service error", StatusCodes.Status502BadGateway);
    public static readonly IErrorCode TokenRefreshFailed = new ErrorCode("Token refresh failed", StatusCodes.Status401Unauthorized);
    public static readonly IErrorCode LogoutFailed = new ErrorCode("Logout failed", StatusCodes.Status400BadRequest);
    public static readonly IErrorCode CognitoSubMissing = new ErrorCode("Cognito Sub is missing", StatusCodes.Status400BadRequest);
}
