using System.Net;


namespace PublicationQualitySystem.Shared.Exceptions;

public static class ResearchGroupErrorCode
{
    public static readonly IErrorCode ResearchGroupNotFound = new ErrorCode("Research group not found", HttpStatusCode.NotFound);
    public static readonly IErrorCode ResearchGroupNameAlreadyExists = new ErrorCode("Research group name already exists", HttpStatusCode.Conflict);
    public static readonly IErrorCode GroupMemberAlreadyExists = new ErrorCode("Group member already exists", HttpStatusCode.Conflict);
    public static readonly IErrorCode GroupMemberNotFound = new ErrorCode("Group member not found", HttpStatusCode.NotFound);
    public static readonly IErrorCode GroupLeaderRequired = new ErrorCode("Group leader is required", HttpStatusCode.BadRequest);
    public static readonly IErrorCode CannotRemoveOnlyLeader = new ErrorCode("Cannot remove the only group leader", HttpStatusCode.BadRequest);
    public static readonly IErrorCode ValidationError = new ErrorCode("Validation error", HttpStatusCode.BadRequest);
}
