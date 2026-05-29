using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Api.Common;
using PublicationQualitySystem.Application.DTOs.Paper;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Api.Controllers;

[Route("api/papers/{paperId:long}/versions")]
public class PaperVersionController(IPaperVersionService paperVersionService) : ApiBaseController
{
    [HttpPost]
    [Authorize(Policy = "PAPER_VERSION_UPLOAD")]
    public async Task<ActionResult<BaseResponse<PaperVersionResponse>>> Create(
        long paperId,
        [FromBody] CreatePaperVersionRequest request) =>
        OkResponse(await paperVersionService.CreateVersionAsync(paperId, request), "Paper version created successfully");

    [HttpGet]
    [Authorize(Policy = "PAPER_VERSION_READ")]
    public async Task<ActionResult<BaseResponse<List<PaperVersionResponse>>>> GetVersions(long paperId) =>
        OkResponse(await paperVersionService.GetVersionsAsync(paperId), "Paper versions retrieved successfully");

    [HttpGet("{versionId:long}")]
    [Authorize(Policy = "PAPER_VERSION_READ")]
    public async Task<ActionResult<BaseResponse<PaperVersionResponse>>> GetVersion(long paperId, long versionId) =>
        OkResponse(await paperVersionService.GetVersionAsync(paperId, versionId), "Paper version retrieved successfully");

    [HttpPut("{versionId:long}")]
    [Authorize(Policy = "PAPER_VERSION_UPDATE")]
    public async Task<ActionResult<BaseResponse<PaperVersionResponse>>> Update(
        long paperId,
        long versionId,
        [FromBody] UpdatePaperVersionRequest request) =>
        OkResponse(await paperVersionService.UpdateVersionAsync(paperId, versionId, request), "Paper version updated successfully");

    [HttpDelete("{versionId:long}")]
    [Authorize(Policy = "PAPER_VERSION_DELETE")]
    public async Task<ActionResult<BaseResponse<object>>> Delete(long paperId, long versionId)
    {
        await paperVersionService.DeleteVersionAsync(paperId, versionId);
        return OkResponse<object>(null, "Paper version deleted successfully");
    }

    [HttpPost("{versionId:long}/restore")]
    [Authorize(Policy = "PAPER_VERSION_RESTORE")]
    public async Task<ActionResult<BaseResponse<PaperVersionResponse>>> Restore(long paperId, long versionId) =>
        OkResponse(await paperVersionService.RestoreVersionAsync(paperId, versionId), "Paper version restored successfully");
}
