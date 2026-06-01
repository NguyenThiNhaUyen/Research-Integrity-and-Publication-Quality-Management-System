using System.Net;


namespace PublicationQualitySystem.Shared.Exceptions;

public static class RoleErrorCode
{
    public static readonly IErrorCode RoleNotFound = new ErrorCode("Role not found", HttpStatusCode.NotFound);
    public static readonly IErrorCode RoleAlreadyExists = new ErrorCode("Role already exists", HttpStatusCode.Conflict);
    public static readonly IErrorCode RoleNameRequired = new ErrorCode("Role name is required", HttpStatusCode.BadRequest);
    public static readonly IErrorCode RoleInUse = new ErrorCode("Role is in use", HttpStatusCode.Conflict);
    public static readonly IErrorCode CognitoGroupSyncFailed = new ErrorCode("Cognito group sync failed", HttpStatusCode.BadGateway);
    public static readonly IErrorCode CognitoGroupNotFound = new ErrorCode("Cognito group not found", HttpStatusCode.NotFound);
    public static readonly IErrorCode ValidationError = new ErrorCode("Validation error", HttpStatusCode.BadRequest);
}
