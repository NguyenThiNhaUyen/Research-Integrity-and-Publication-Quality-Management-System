using Microsoft.AspNetCore.Mvc;

namespace PublicationQualitySystem.Common;

[ApiController]
public abstract class ApiBaseController : ControllerBase
{
    protected ActionResult<BaseResponse<T>> OkResponse<T>(T? data, string message) =>
        Ok(BaseResponse<T>.SuccessResponse(data, message));

    protected ActionResult<BaseResponse<T>> CreatedResponse<T>(T data) =>
        StatusCode(StatusCodes.Status201Created, BaseResponse<T>.Created(data));
}
