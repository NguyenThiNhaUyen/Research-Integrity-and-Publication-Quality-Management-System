using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Api.Common;
using PublicationQualitySystem.Application.DTOs.Paper;
using PublicationQualitySystem.Application.DTOs.Processing;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Api.Controllers;

[Route("api/paper-versions")]
public class PaperVersionController(
    IPaperVersionService paperVersionService,
    IPaperProcessingTrackerService processingTrackerService,
    ILogger<PaperVersionController> logger) : ApiBaseController
{
    [HttpGet("/api/papers/{paperId:long}/versions")]
    public async Task<ActionResult<BaseResponse<IReadOnlyList<PaperVersionResponse>>>> GetVersionsByPaper(
        long paperId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Paper versions request received. PaperId={PaperId}", paperId);

        var result = await paperVersionService.GetVersionsByPaperIdAsync(paperId, cancellationToken);

        return OkResponse(result, "Paper versions retrieved successfully");
    }

    [HttpGet("{paperVersionId:long}")]
    public async Task<ActionResult<BaseResponse<PaperVersionResponse>>> GetVersion(
        long paperVersionId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("PaperVersion request received. PaperVersionId={PaperVersionId}", paperVersionId);

        var result = await paperVersionService.GetVersionAsync(paperVersionId, cancellationToken);

        return OkResponse(result, "Paper version retrieved successfully");
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
