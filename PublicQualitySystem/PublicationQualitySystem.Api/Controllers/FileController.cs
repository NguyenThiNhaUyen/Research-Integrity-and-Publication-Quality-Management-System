using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Api.Common;
using PublicationQualitySystem.Application.DTOs.File;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Api.Controllers;

[Route("api/files")]
public class FileController(
    IPaperVersionService paperVersionService,
    IUploadService uploadService) : ApiBaseController
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<BaseResponse<List<UploadedFileResponse>>>> GetFiles() =>
        OkResponse(await uploadService.GetFilesAsync(), "Files retrieved successfully");

    [HttpGet("{fileId:long}")]
    [Authorize]
    public async Task<ActionResult<BaseResponse<UploadedFileResponse>>> GetFile(long fileId) =>
        OkResponse(await uploadService.GetFileAsync(fileId), "File retrieved successfully");

    [HttpGet("{fileId:long}/download-url")]
    [Authorize(Policy = "PAPER_VERSION_READ")]
    public async Task<ActionResult<BaseResponse<DownloadUrlResponse>>> GetDownloadUrl(long fileId) =>
        OkResponse(await paperVersionService.GetDownloadUrlAsync(fileId), "Download URL generated successfully");
}
