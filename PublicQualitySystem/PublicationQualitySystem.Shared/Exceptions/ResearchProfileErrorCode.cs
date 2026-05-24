
namespace PublicationQualitySystem.Shared.Exceptions;

public static class ResearchProfileErrorCode
{
    public static readonly IErrorCode ProfileNotFound = new ErrorCode("Research profile not found", 404);
    public static readonly IErrorCode ValidationError = new ErrorCode("Validation error", 400);
}
