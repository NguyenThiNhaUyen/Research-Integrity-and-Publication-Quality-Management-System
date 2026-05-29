using System.Net;


namespace PublicationQualitySystem.Shared.Exceptions;

public static class ResearchProfileErrorCode
{
    public static readonly IErrorCode ProfileNotFound = new ErrorCode("Research profile not found", HttpStatusCode.NotFound);
    public static readonly IErrorCode ValidationError = new ErrorCode("Validation error", HttpStatusCode.BadRequest);
}
