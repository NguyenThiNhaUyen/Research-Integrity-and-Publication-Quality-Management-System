namespace PublicationQualitySystem.Shared.Exceptions;

public static class PaperReviewProfileErrorCode
{
    public static readonly IErrorCode PaperNotFound = new ErrorCode("Paper not found", 404);
    public static readonly IErrorCode ProfileNotFound = new ErrorCode("Paper review profile not found", 404);
    public static readonly IErrorCode Forbidden = new ErrorCode("You are not allowed to access this paper review profile", 403);
    public static readonly IErrorCode ValidationError = new ErrorCode("Paper review profile validation error", 400);
}
