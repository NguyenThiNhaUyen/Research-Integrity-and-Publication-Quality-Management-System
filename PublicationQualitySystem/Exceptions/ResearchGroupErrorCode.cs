using PublicationQualitySystem.Enums;

namespace PublicationQualitySystem.Exceptions;

public static class ResearchGroupErrorCode
{
    public static readonly IErrorCode ResearchGroupNotFound = new ErrorCode("Research group not found", StatusCodes.Status404NotFound);
    public static readonly IErrorCode ResearchGroupNameAlreadyExists = new ErrorCode("Research group name already exists", StatusCodes.Status409Conflict);
    public static readonly IErrorCode GroupMemberAlreadyExists = new ErrorCode("Group member already exists", StatusCodes.Status409Conflict);
    public static readonly IErrorCode GroupMemberNotFound = new ErrorCode("Group member not found", StatusCodes.Status404NotFound);
    public static readonly IErrorCode GroupLeaderRequired = new ErrorCode("Group leader is required", StatusCodes.Status400BadRequest);
    public static readonly IErrorCode CannotRemoveOnlyLeader = new ErrorCode("Cannot remove the only group leader", StatusCodes.Status400BadRequest);
    public static readonly IErrorCode ValidationError = new ErrorCode("Validation error", StatusCodes.Status400BadRequest);
}
