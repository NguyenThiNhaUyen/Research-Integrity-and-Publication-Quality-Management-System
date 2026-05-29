using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Api.Common;
using PublicationQualitySystem.Shared.Common;
using PublicationQualitySystem.Application.DTOs.File.Requests;
using PublicationQualitySystem.Application.DTOs.File.Responses;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Application.Services.Interfaces;

namespace PublicationQualitySystem.Api.Controllers;

[Route("api/uploads")]
public class UploadController(IUploadService uploadService) : ApiBaseController
{
    [HttpPost]
    [Authorize(Policy = "FILE_UPLOAD")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<BaseResponse<S3FileResponseDto>>> Upload(
        IFormFile file,
        [FromForm] UploadType type) =>
        OkResponse(await uploadService.UploadAsync(new FileUploadRequestDto
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length,
            Content = file.OpenReadStream()
        }, type), "Upload file successfully");
}
