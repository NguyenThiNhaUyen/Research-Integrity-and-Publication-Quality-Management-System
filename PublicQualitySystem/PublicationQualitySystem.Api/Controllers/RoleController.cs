using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Api.Common;
using PublicationQualitySystem.Shared.Common;
using PublicationQualitySystem.Application.DTOs.Role.Requests;
using PublicationQualitySystem.Application.DTOs.Role.Responses;
using PublicationQualitySystem.Application.Services.Interfaces;

namespace PublicationQualitySystem.Api.Controllers;

[Route("api/roles")]
public class RoleController(IRoleService roleService) : ApiBaseController
{
    [HttpPost]
    public async Task<ActionResult<BaseResponse<RoleResponseDto>>> Create([FromBody] CreateRoleRequestDto dto) => CreatedResponse(await roleService.CreateAsync(dto));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<BaseResponse<RoleResponseDto>>> GetById(long id) => OkResponse(await roleService.GetByIdAsync(id), "Get by id successfully");

    [HttpPut("{id:long}")]
    public async Task<ActionResult<BaseResponse<RoleResponseDto>>> Update(long id, [FromBody] UpdateRoleRequestDto dto) => OkResponse(await roleService.UpdateAsync(id, dto), "Update successfully");

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<BaseResponse<object>>> Delete(long id)
    {
        await roleService.DeleteAsync(id);
        return OkResponse<object>(null, "Delete successfully");
    }

    [HttpGet]
    [Authorize(Policy = "ROLE_READ")]
    public async Task<ActionResult<BaseResponse<List<RoleResponseDto>>>> GetAll([FromQuery] int page = 0, [FromQuery] int size = 20) =>
        OkResponse(await roleService.GetAllRolesAsync(page, size), "Roles retrieved successfully");

    [HttpGet("users/{userId}")]
    [Authorize(Policy = "ROLE_READ")]
    public async Task<ActionResult<BaseResponse<List<UserRoleResponseDto>>>> GetUserRoles(string userId) =>
        OkResponse(await roleService.GetUserRolesAsync(userId), "User roles retrieved successfully");

    [HttpPost("users/{userId}/{roleId:long}")]
    [Authorize(Policy = "ROLE_ASSIGN")]
    public async Task<ActionResult<BaseResponse<List<UserRoleResponseDto>>>> AssignRole(string userId, long roleId) =>
        OkResponse(await roleService.AssignRoleToUserAsync(userId, roleId), "Role assigned successfully");

    [HttpDelete("users/{userId}/{roleId:long}")]
    [Authorize(Policy = "ROLE_ASSIGN")]
    public async Task<ActionResult<BaseResponse<List<UserRoleResponseDto>>>> RemoveRole(string userId, long roleId) =>
        OkResponse(await roleService.RemoveRoleFromUserAsync(userId, roleId), "Role removed successfully");

    [HttpPut("users/{userId}")]
    [Authorize(Policy = "ROLE_ASSIGN")]
    public async Task<ActionResult<BaseResponse<List<UserRoleResponseDto>>>> ReplaceRoles(string userId, [FromBody] UpdateUserRolesRequestDto dto) =>
        OkResponse(await roleService.ReplaceUserRolesAsync(userId, dto), "User roles updated successfully");
}
