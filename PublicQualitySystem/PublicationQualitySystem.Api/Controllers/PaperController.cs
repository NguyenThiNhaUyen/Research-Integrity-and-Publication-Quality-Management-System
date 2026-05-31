using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PublicationQualitySystem.Api.Common;
using PublicationQualitySystem.Application.DTOs.Paper;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Api.Controllers;

[Route("api/papers")]
public class PaperController(IPaperService paperService, ILogger<PaperController> logger) : ApiBaseController
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
}
