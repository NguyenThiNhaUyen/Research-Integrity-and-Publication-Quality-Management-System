using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PublicationQualitySystem.Api.Common;
using PublicationQualitySystem.Application.DTOs.Audit;
using PublicationQualitySystem.Application.DTOs.Paper;
using PublicationQualitySystem.Application.DTOs.Processing;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Api.Controllers;

[Route("api/papers")]
public class PaperController(
    IPaperService paperService,
    IAuditLogService auditLogService,
    IPaperProcessingTrackerService processingTrackerService,
    ILogger<PaperController> logger) : ApiBaseController
{
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<BaseResponse<PaperVersionResponse>>> Upload(
        [FromForm] UploadPaperRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Paper upload request received. FileName={FileName}, ContentType={ContentType}, Length={Length}, HasTitle={HasTitle}",
            request.File.FileName,
            request.File.ContentType,
            request.File.Length,
            !string.IsNullOrWhiteSpace(request.Title));

        await using var stream = request.File.OpenReadStream();
        var result = await paperService.UploadPaperAsync(
            stream,
            request.File.FileName,
            request.File.ContentType,
            request.Title,
            cancellationToken);

        logger.LogInformation(
            "Paper upload request completed. PaperId={PaperId}, PaperVersionId={PaperVersionId}, ConversionStatus={ConversionStatus}",
            result.PaperId,
            result.PaperVersionId,
            result.ConversionStatus);

        return CreatedResponse(result);
    }

    [HttpGet("{paperId:long}/metadata")]
    public async Task<ActionResult<BaseResponse<PaperMetadataResponse>>> GetMetadata(
        long paperId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Paper metadata request received. PaperId={PaperId}", paperId);

        var result = await paperService.GetMetadataAsync(paperId, cancellationToken);

        logger.LogInformation(
            "Paper metadata request completed. PaperId={PaperId}, ExtractionStatus={ExtractionStatus}",
            result.PaperId,
            result.ExtractionStatus);

        return OkResponse(result, "Success");
    }

    [HttpGet("{paperId:long}/audit-logs")]
    public async Task<ActionResult<BaseResponse<IReadOnlyList<AuditLogResponse>>>> GetAuditLogs(
        long paperId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Paper audit logs request received. PaperId={PaperId}", paperId);

        var result = await auditLogService.GetLogsByPaperIdAsync(paperId, cancellationToken);

        return OkResponse(result, "Success");
    }

    [HttpGet("{paperId:long}/processing-progress")]
    public async Task<ActionResult<BaseResponse<ProcessingProgressResponse>>> GetProcessingProgress(
        long paperId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Paper processing progress request received. PaperId={PaperId}", paperId);

        var result = await auditLogService.GetProcessingProgressByPaperIdAsync(paperId, cancellationToken);

        return OkResponse(result, "Success");
    }

    [HttpGet("{paperId:long}/processing-tracker")]
    public async Task<ActionResult<BaseResponse<PaperProcessingTrackerResponse>>> GetProcessingTracker(
        long paperId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Paper processing tracker request received. PaperId={PaperId}", paperId);

        var result = await processingTrackerService.GetByPaperIdAsync(paperId, cancellationToken);

        return OkResponse(result, "Success");
    }
}
