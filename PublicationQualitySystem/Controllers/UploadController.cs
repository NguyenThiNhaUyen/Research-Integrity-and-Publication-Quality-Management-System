using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Common;
using PublicationQualitySystem.DTOs.File;
using PublicationQualitySystem.Enums;
using PublicationQualitySystem.Services.Interfaces;

namespace PublicationQualitySystem.Controllers;

[Route("api/uploads")]
public class UploadController(IUploadService uploadService) : ApiBaseController
{
    [HttpPost]
    [Authorize(Policy = "FILE_UPLOAD")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<BaseResponse<S3FileResponseDto>>> Upload(
        IFormFile file,
        [FromForm] UploadType type) =>
        OkResponse(await uploadService.UploadAsync(file, type), "Upload file successfully");
}
