
namespace PublicationQualitySystem.Shared.Exceptions;

public static class ResearchGroupErrorCode
{
    public static readonly IErrorCode ResearchGroupNotFound = new ErrorCode("Research group not found", 404);
    public static readonly IErrorCode ResearchGroupNameAlreadyExists = new ErrorCode("Research group name already exists", 409);
    public static readonly IErrorCode GroupMemberAlreadyExists = new ErrorCode("Group member already exists", 409);
    public static readonly IErrorCode GroupMemberNotFound = new ErrorCode("Group member not found", 404);
    public static readonly IErrorCode GroupLeaderRequired = new ErrorCode("Group leader is required", 400);
    public static readonly IErrorCode CannotRemoveOnlyLeader = new ErrorCode("Cannot remove the only group leader", 400);
    public static readonly IErrorCode ValidationError = new ErrorCode("Validation error", 400);
}
