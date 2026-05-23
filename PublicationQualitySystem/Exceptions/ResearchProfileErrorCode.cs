namespace PublicationQualitySystem.Exceptions;

public static class ResearchProfileErrorCode
{
    public static readonly IErrorCode ProfileNotFound = new ErrorCode("Research profile not found", StatusCodes.Status404NotFound);
    public static readonly IErrorCode ValidationError = new ErrorCode("Validation error", StatusCodes.Status400BadRequest);
}
