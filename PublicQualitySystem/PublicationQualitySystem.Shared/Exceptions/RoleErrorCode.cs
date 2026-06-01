
namespace PublicationQualitySystem.Shared.Exceptions;

public static class RoleErrorCode
{
    public static readonly IErrorCode RoleNotFound = new ErrorCode("Role not found", 404);
    public static readonly IErrorCode RoleAlreadyExists = new ErrorCode("Role already exists", 409);
    public static readonly IErrorCode RoleNameRequired = new ErrorCode("Role name is required", 400);
    public static readonly IErrorCode RoleInUse = new ErrorCode("Role is in use", 409);
    public static readonly IErrorCode CognitoGroupSyncFailed = new ErrorCode("Cognito group sync failed", 502);
    public static readonly IErrorCode CognitoGroupNotFound = new ErrorCode("Cognito group not found", 404);
    public static readonly IErrorCode ValidationError = new ErrorCode("Validation error", 400);
}
