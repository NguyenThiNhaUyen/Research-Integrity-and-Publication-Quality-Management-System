using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Api.Common;
using PublicationQualitySystem.Shared.Common;
using PublicationQualitySystem.Application.Services.Interfaces;

namespace PublicationQualitySystem.Api.Controllers;

[Route("api/s3")]
public class S3Controller(IFileStorageService fileStorageService) : ApiBaseController
{
    [HttpDelete]
    [Authorize(Policy = "FILE_DELETE")]
    public async Task<ActionResult<BaseResponse<object>>> Delete([FromQuery] string fileKey)
    {
        await fileStorageService.DeleteAsync(fileKey);
        return OkResponse<object>(null, "File deleted successfully");
    }
}
