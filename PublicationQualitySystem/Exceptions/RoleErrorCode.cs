namespace PublicationQualitySystem.Exceptions;

public static class RoleErrorCode
{
    public static readonly IErrorCode RoleNotFound = new ErrorCode("Role not found", StatusCodes.Status404NotFound);
    public static readonly IErrorCode RoleAlreadyExists = new ErrorCode("Role already exists", StatusCodes.Status409Conflict);
    public static readonly IErrorCode RoleNameRequired = new ErrorCode("Role name is required", StatusCodes.Status400BadRequest);
    public static readonly IErrorCode RoleInUse = new ErrorCode("Role is in use", StatusCodes.Status409Conflict);
    public static readonly IErrorCode CognitoGroupSyncFailed = new ErrorCode("Cognito group sync failed", StatusCodes.Status502BadGateway);
    public static readonly IErrorCode CognitoGroupNotFound = new ErrorCode("Cognito group not found", StatusCodes.Status404NotFound);
    public static readonly IErrorCode ValidationError = new ErrorCode("Validation error", StatusCodes.Status400BadRequest);
}
