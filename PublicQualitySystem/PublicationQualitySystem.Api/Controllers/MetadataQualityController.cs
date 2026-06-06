using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Api.Common;
using PublicationQualitySystem.Application.DTOs.Metadata;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Api.Controllers;

[Route("api/paper-versions/{versionId:long}/metadata-quality")]
public class MetadataQualityController(
    IMetadataQualityService metadataQualityService,
    ILogger<MetadataQualityController> logger) : ApiBaseController
{
    [HttpGet]
    public async Task<ActionResult<BaseResponse<MetadataQualityResponse>>> GetMetadataQuality(
        long versionId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Metadata quality request received. PaperVersionId={PaperVersionId}", versionId);

        var result = await metadataQualityService.GetByPaperVersionIdAsync(versionId, cancellationToken);

        return OkResponse(result, "Metadata quality retrieved successfully");
    }
}
