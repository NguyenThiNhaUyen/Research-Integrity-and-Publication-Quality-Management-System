using System.Net;
using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Shared.Exceptions;

public static class AuditLogErrorCode
{
    public static readonly IErrorCode Forbidden = new ErrorCode("You are not allowed to view these audit logs.", HttpStatusCode.Forbidden);
    public static readonly IErrorCode NotFound = new ErrorCode("Audit log target was not found.", HttpStatusCode.NotFound);
}
