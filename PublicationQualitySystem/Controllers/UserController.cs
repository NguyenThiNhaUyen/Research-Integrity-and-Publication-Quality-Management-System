using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Common;
using PublicationQualitySystem.DTOs.Auth;
using PublicationQualitySystem.DTOs.File;
using PublicationQualitySystem.DTOs.ResearchGroup;
using PublicationQualitySystem.DTOs.ResearchProfile;
using PublicationQualitySystem.DTOs.Role;
using PublicationQualitySystem.DTOs.User;
using PublicationQualitySystem.Services.Interfaces;

namespace PublicationQualitySystem.Controllers;

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
