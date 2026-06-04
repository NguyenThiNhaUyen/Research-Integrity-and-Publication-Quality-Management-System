using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Api.Common;
using PublicationQualitySystem.Application.DTOs.Audit;
using PublicationQualitySystem.Application.DTOs.Processing;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Api.Controllers;

[Route("api/paper-versions")]
public class PaperVersionController(
    IAuditLogService auditLogService,
    IPaperProcessingTrackerService processingTrackerService,
    ILogger<PaperVersionController> logger) : ApiBaseController
{
    [HttpGet("{paperVersionId:long}/audit-logs")]
    public async Task<ActionResult<BaseResponse<IReadOnlyList<AuditLogResponse>>>> GetAuditLogs(
        long paperVersionId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("PaperVersion audit logs request received. PaperVersionId={PaperVersionId}", paperVersionId);

        var result = await auditLogService.GetLogsByPaperVersionIdAsync(paperVersionId, cancellationToken);

        return OkResponse(result, "Success");
    }

    [HttpGet("{paperVersionId:long}/processing-tracker")]
    public async Task<ActionResult<BaseResponse<PaperProcessingTrackerResponse>>> GetProcessingTracker(
        long paperVersionId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("PaperVersion processing tracker request received. PaperVersionId={PaperVersionId}", paperVersionId);

        var result = await processingTrackerService.GetByPaperVersionIdAsync(paperVersionId, cancellationToken);

        return OkResponse(result, "Success");
    }
}
