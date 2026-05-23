using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Common;
using PublicationQualitySystem.DTOs;
using PublicationQualitySystem.Services;

namespace PublicationQualitySystem.Controllers;

[Route("api/lab-members/files")]
public class S3FileController(IS3FileService s3FileService) : ApiBaseController
{
    [HttpPost("upload")]
    [Authorize(Policy = "FILE_UPLOAD")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<BaseResponse<S3FileResponseDto>>> UploadFile([FromForm] UploadFileRequest request) =>
        OkResponse(await s3FileService.UploadFileAsync(request.File), "Upload file successfully");

    [HttpDelete]
    [Authorize(Policy = "FILE_DELETE")]
    public async Task<ActionResult<BaseResponse<object>>> DeleteFile([FromQuery] string fileKey)
    {
        await s3FileService.DeleteFileAsync(fileKey);
        return OkResponse<object>(null, "File deleted successfully");
    }
}
