using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Api.Common;
using PublicationQualitySystem.Application.DTOs.Audit;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Api.Controllers;

[Route("api/paper-versions/{versionId:long}/audit-logs")]
public class AuditLogController(
    IAuditLogService auditLogService,
    ILogger<AuditLogController> logger) : ApiBaseController
{
    [HttpGet]
    public async Task<ActionResult<BaseResponse<IReadOnlyList<AuditLogResponse>>>> GetAuditLogs(
        long versionId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Audit logs request received. PaperVersionId={PaperVersionId}", versionId);

        var result = await auditLogService.GetLogsByPaperVersionIdAsync(versionId, cancellationToken);

        return OkResponse(result, "Audit logs retrieved successfully");
    }
}
