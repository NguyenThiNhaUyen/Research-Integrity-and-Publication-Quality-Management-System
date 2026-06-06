using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Api.Common;
using PublicationQualitySystem.Application.DTOs.OpenAlex;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Api.Controllers;

[Route("api/paper-versions/{versionId:long}/similarity")]
public class OpenAlexController(
    IOpenAlexService openAlexService,
    ILogger<OpenAlexController> logger) : ApiBaseController
{
    [HttpGet]
    public async Task<ActionResult<BaseResponse<SimilarityResultResponse>>> GetSimilarity(
        long versionId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("OpenAlex similarity request received. PaperVersionId={PaperVersionId}", versionId);

        var result = await openAlexService.GetSimilarityByPaperVersionIdAsync(versionId, cancellationToken);

        return OkResponse(result, "Similarity result retrieved successfully");
    }
}
