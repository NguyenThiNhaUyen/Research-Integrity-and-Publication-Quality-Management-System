using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Api.Common;
using PublicationQualitySystem.Application.DTOs.Paper;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Api.Controllers;

[Route("api/papers/{paperId:long}/versions")]
public class PaperVersionController(IPaperVersionService paperVersionService) : BaseCrudController<
    CreatePaperVersionRequest,
    UpdatePaperVersionRequest,
    PaperVersionResponse,
    long>
{
    protected override string? CreatePolicy => "PAPER_VERSION_UPLOAD";
    protected override string? UpdatePolicy => "PAPER_VERSION_UPDATE";
    protected override string? GetByIdPolicy => "PAPER_VERSION_READ";
    protected override string? DeletePolicy => "PAPER_VERSION_DELETE";

    [HttpGet]
    [Authorize(Policy = "PAPER_VERSION_READ")]
    public async Task<ActionResult<BaseResponse<List<PaperVersionResponse>>>> GetVersions(long paperId) =>
        OkResponse(await paperVersionService.GetVersionsAsync(paperId), "Paper versions retrieved successfully");

    [HttpPost("{versionId:long}/restore")]
    [Authorize(Policy = "PAPER_VERSION_RESTORE")]
    public async Task<ActionResult<BaseResponse<PaperVersionResponse>>> Restore(long paperId, long versionId) =>
        OkResponse(await paperVersionService.RestoreVersionAsync(paperId, versionId), "Paper version restored successfully");

    protected override Task<PaperVersionResponse> CreateEntityAsync(CreatePaperVersionRequest request) =>
        paperVersionService.CreateVersionAsync(GetPaperId(), request);

    protected override Task<PaperVersionResponse> GetEntityByIdAsync(long id) =>
        paperVersionService.GetVersionAsync(GetPaperId(), id);

    protected override Task<PaperVersionResponse> UpdateEntityAsync(long id, UpdatePaperVersionRequest request) =>
        paperVersionService.UpdateVersionAsync(GetPaperId(), id, request);

    protected override Task DeleteEntityAsync(long id) =>
        paperVersionService.DeleteVersionAsync(GetPaperId(), id);

    private long GetPaperId() => long.Parse(RouteData.Values["paperId"]?.ToString() ?? "0");
}
