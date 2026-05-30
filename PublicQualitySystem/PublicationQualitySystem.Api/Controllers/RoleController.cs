using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Api.Common;
using PublicationQualitySystem.Shared.Common;

using PublicationQualitySystem.Application.DTOs.Role;
using PublicationQualitySystem.Application.DTOs.User;
using PublicationQualitySystem.Application.Services.Interfaces;

namespace PublicationQualitySystem.Api.Controllers;

[Route("api/roles")]
public class RoleController(IRoleService roleService) : BaseCrudController<
    CreateRoleRequest,
    UpdateRoleRequest,
    RoleResponse,
    long>(roleService)
{
    [HttpGet]
    [Authorize(Policy = "ROLE_READ")]
    public async Task<ActionResult<BaseResponse<List<RoleResponse>>>> GetAll([FromQuery] int page = 0, [FromQuery] int size = 20) =>
        OkResponse(await roleService.GetAllRolesAsync(page, size), "Roles retrieved successfully");

    [HttpGet("users/{userId}")]
    [Authorize(Policy = "ROLE_READ")]
    public async Task<ActionResult<BaseResponse<List<UserRoleResponse>>>> GetUserRoles(string userId) =>
        OkResponse(await roleService.GetUserRolesAsync(userId), "User roles retrieved successfully");

    [HttpPost("users/{userId}/{roleId:long}")]
    [Authorize(Policy = "ROLE_ASSIGN")]
    public async Task<ActionResult<BaseResponse<List<UserRoleResponse>>>> AssignRole(string userId, long roleId) =>
        OkResponse(await roleService.AssignRoleToUserAsync(userId, roleId), "Role assigned successfully");

    [HttpDelete("users/{userId}/{roleId:long}")]
    [Authorize(Policy = "ROLE_ASSIGN")]
    public async Task<ActionResult<BaseResponse<List<UserRoleResponse>>>> RemoveRole(string userId, long roleId) =>
        OkResponse(await roleService.RemoveRoleFromUserAsync(userId, roleId), "Role removed successfully");

    [HttpPut("users/{userId}")]
    [Authorize(Policy = "ROLE_ASSIGN")]
    public async Task<ActionResult<BaseResponse<List<UserRoleResponse>>>> ReplaceRoles(string userId, [FromBody] UpdateUserRolesRequest dto) =>
        OkResponse(await roleService.ReplaceUserRolesAsync(userId, dto), "User roles updated successfully");
}
