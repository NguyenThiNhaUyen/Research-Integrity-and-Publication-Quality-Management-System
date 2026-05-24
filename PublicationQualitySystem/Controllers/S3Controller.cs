using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Common;
using PublicationQualitySystem.Services.Interfaces;

namespace PublicationQualitySystem.Controllers;

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
