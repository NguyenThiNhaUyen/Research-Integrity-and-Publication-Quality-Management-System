using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Api.Common;
using PublicationQualitySystem.Shared.Common;
using PublicationQualitySystem.Application.DTOs.User.Requests;
using PublicationQualitySystem.Application.DTOs.User.Responses;
using PublicationQualitySystem.Application.Services.Interfaces;

namespace PublicationQualitySystem.Api.Controllers;

[Route("api/lab-members/users")]
public class UserController(IUserService userService) : ApiBaseController
{
    [HttpPost]
    public async Task<ActionResult<BaseResponse<UserResponseDto>>> Create([FromBody] CreateUserRequestDto dto) => CreatedResponse(await userService.CreateAsync(dto));

    [HttpGet("{id}")]
    public async Task<ActionResult<BaseResponse<UserResponseDto>>> GetById(string id) => OkResponse(await userService.GetByIdAsync(id), "Get by id successfully");

    [HttpPut("{id}")]
    public async Task<ActionResult<BaseResponse<UserResponseDto>>> Update(string id, [FromBody] UpdateUserRequestDto dto) => OkResponse(await userService.UpdateAsync(id, dto), "Update successfully");

    [HttpDelete("{id}")]
    public async Task<ActionResult<BaseResponse<object>>> Delete(string id)
    {
        await userService.DeleteAsync(id);
        return OkResponse<object>(null, "Delete successfully");
    }
}
