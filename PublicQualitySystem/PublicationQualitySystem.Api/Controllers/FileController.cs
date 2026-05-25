using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Api.Common;
using PublicationQualitySystem.Application.DTOs.File;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Api.Controllers;

[Route("api/files")]
public class FileController(IPaperVersionService paperVersionService) : ApiBaseController
{
    [HttpGet("{fileId:long}/download-url")]
    [Authorize(Policy = "PAPER_VERSION_READ")]
    public async Task<ActionResult<BaseResponse<DownloadUrlResponseDto>>> GetDownloadUrl(long fileId) =>
        OkResponse(await paperVersionService.GetDownloadUrlAsync(fileId), "Download URL generated successfully");
}
