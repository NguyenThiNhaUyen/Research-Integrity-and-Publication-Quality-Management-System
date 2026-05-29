using Microsoft.AspNetCore.Authorization;
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

[Route("api/lab-members/research-groups")]
public class ResearchGroupController(IResearchGroupService groupService) : BaseCrudController<
    CreateResearchGroupRequest,
    UpdateResearchGroupRequest,
    ResearchGroupResponse,
    long>(groupService)
{
    [HttpGet]
    [Authorize(Policy = "RESEARCH_GROUP_READ")]
    public async Task<ActionResult<BaseResponse<List<ResearchGroupResponse>>>> GetAll([FromQuery] int page = 0, [FromQuery] int size = 20) =>
        OkResponse(await groupService.GetAllAsync(page, size), "Research groups retrieved successfully");

    [HttpPost("{groupId:long}/members")]
    [Authorize(Policy = "RESEARCH_GROUP_MEMBER_MANAGE")]
    public async Task<ActionResult<BaseResponse<ResearchGroupMemberResponse>>> AddMember(long groupId, [FromBody] ResearchGroupMemberRequest dto) =>
        CreatedResponse(await groupService.AddMemberAsync(groupId, dto));

    [HttpGet("{groupId:long}/members")]
    [Authorize(Policy = "RESEARCH_GROUP_READ")]
    public async Task<ActionResult<BaseResponse<List<ResearchGroupMemberResponse>>>> GetMembers(long groupId) =>
        OkResponse(await groupService.GetGroupMembersAsync(groupId), "Research group members retrieved successfully");

    [HttpDelete("{groupId:long}/members/{userId}")]
    [Authorize(Policy = "RESEARCH_GROUP_MEMBER_MANAGE")]
    public async Task<ActionResult<BaseResponse<object>>> RemoveMember(long groupId, string userId)
    {
        await groupService.RemoveMemberAsync(groupId, userId);
        return OkResponse<object>(null, "Research group member removed successfully");
    }

    [HttpPatch("{groupId:long}/members/{userId}/role")]
    [Authorize(Policy = "RESEARCH_GROUP_MEMBER_MANAGE")]
    public async Task<ActionResult<BaseResponse<ResearchGroupMemberResponse>>> ChangeMemberRole(long groupId, string userId, [FromBody] ResearchGroupMemberRequest dto) =>
        OkResponse(await groupService.ChangeMemberRoleAsync(groupId, userId, dto), "Research group member role updated successfully");

    [HttpPatch("{groupId:long}/members/{userId}/status")]
    [Authorize(Policy = "RESEARCH_GROUP_MEMBER_MANAGE")]
    public async Task<ActionResult<BaseResponse<ResearchGroupMemberResponse>>> UpdateMemberStatus(long groupId, string userId, [FromBody] ResearchGroupMemberRequest dto) =>
        OkResponse(await groupService.UpdateMemberStatusAsync(groupId, userId, dto), "Research group member status updated successfully");

    [HttpPatch("{groupId:long}/leader/{userId}")]
    [Authorize(Policy = "RESEARCH_GROUP_MEMBER_MANAGE")]
    public async Task<ActionResult<BaseResponse<ResearchGroupMemberResponse>>> AssignLeader(long groupId, string userId) =>
        OkResponse(await groupService.AssignGroupLeaderAsync(groupId, userId), "Research group leader assigned successfully");

    [HttpGet("users/{userId}")]
    [Authorize(Policy = "RESEARCH_GROUP_READ")]
    public async Task<ActionResult<BaseResponse<List<ResearchGroupResponse>>>> GetGroupsByUser(string userId) =>
        OkResponse(await groupService.GetGroupsByUserAsync(userId), "User research groups retrieved successfully");
}
