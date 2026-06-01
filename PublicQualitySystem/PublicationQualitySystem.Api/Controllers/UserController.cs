using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Api.Common;
using PublicationQualitySystem.Shared.Common;
using PublicationQualitySystem.Application.DTOs.Auth;
using PublicationQualitySystem.Application.DTOs.File;
using PublicationQualitySystem.Application.DTOs.ResearchGroup;
using PublicationQualitySystem.Application.DTOs.ResearchProfile;
using PublicationQualitySystem.Application.DTOs.Role;
using PublicationQualitySystem.Application.DTOs.User;
using PublicationQualitySystem.Application.Services.Interfaces;

namespace PublicationQualitySystem.Api.Controllers;

[Route("api/lab-members/users")]
public class UserController(IUserService userService) : ApiBaseController
{
    [HttpPost]
    public async Task<ActionResult<BaseResponse<UserDto>>> Create([FromBody] UserDto dto) => CreatedResponse(await userService.CreateAsync(dto));

    [HttpGet("{id}")]
    public async Task<ActionResult<BaseResponse<UserDto>>> GetById(string id) => OkResponse(await userService.GetByIdAsync(id), "Get by id successfully");

    [HttpPut("{id}")]
    public async Task<ActionResult<BaseResponse<UserDto>>> Update(string id, [FromBody] UserDto dto) => OkResponse(await userService.UpdateAsync(id, dto), "Update successfully");

    [HttpDelete("{id}")]
    public async Task<ActionResult<BaseResponse<object>>> Delete(string id)
    {
        await userService.DeleteAsync(id);
        return OkResponse<object>(null, "Delete successfully");
    }
}
