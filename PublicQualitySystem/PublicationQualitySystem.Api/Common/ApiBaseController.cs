using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Api.Common;

[ApiController]
public abstract class ApiBaseController : ControllerBase
{
    protected ActionResult<BaseResponse<T>> OkResponse<T>(T? data, string message) =>
        Ok(BaseResponse<T>.SuccessResponse(data, message));

    protected ActionResult<BaseResponse<T>> CreatedResponse<T>(T data) =>
        StatusCode(201, BaseResponse<T>.Created(data));
}
