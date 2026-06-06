using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Api.Common;
using PublicationQualitySystem.Application.DTOs.Processing;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Api.Controllers;

[Route("api/paper-versions/{versionId:long}/tracker")]
public class ProcessingTrackerController(
    IProcessingTrackerService processingTrackerService,
    ILogger<ProcessingTrackerController> logger) : ApiBaseController
{
    [HttpGet]
    public async Task<ActionResult<BaseResponse<PaperProcessingTrackerResponse>>> GetTracker(
        long versionId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing tracker request received. PaperVersionId={PaperVersionId}", versionId);

        var result = await processingTrackerService.GetByPaperVersionIdAsync(versionId, cancellationToken);

        return OkResponse(result, "Processing tracker retrieved successfully");
    }
}
