using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Common;
using PublicationQualitySystem.DTOs;
using PublicationQualitySystem.Services;

namespace PublicationQualitySystem.Controllers;

[Route("api/lab-members/research-groups")]
public class ResearchGroupController(IResearchGroupService groupService) : ApiBaseController
{
    [HttpPost]
    public async Task<ActionResult<BaseResponse<ResearchGroupDto>>> Create([FromBody] ResearchGroupDto dto) => CreatedResponse(await groupService.CreateAsync(dto));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<BaseResponse<ResearchGroupDto>>> GetById(long id) => OkResponse(await groupService.GetByIdAsync(id), "Get by id successfully");

    [HttpPut("{id:long}")]
    public async Task<ActionResult<BaseResponse<ResearchGroupDto>>> Update(long id, [FromBody] ResearchGroupDto dto) => OkResponse(await groupService.UpdateAsync(id, dto), "Update successfully");

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<BaseResponse<object>>> Delete(long id)
    {
        await groupService.DeleteAsync(id);
        return OkResponse<object>(null, "Delete successfully");
    }

    [HttpGet]
    [Authorize(Policy = "RESEARCH_GROUP_READ")]
    public async Task<ActionResult<BaseResponse<List<ResearchGroupDto>>>> GetAll([FromQuery] int page = 0, [FromQuery] int size = 20) =>
        OkResponse(await groupService.GetAllAsync(page, size), "Research groups retrieved successfully");

    [HttpPost("{groupId:long}/members")]
    [Authorize(Policy = "RESEARCH_GROUP_MEMBER_MANAGE")]
    public async Task<ActionResult<BaseResponse<ResearchGroupMemberDto>>> AddMember(long groupId, [FromBody] ResearchGroupMemberDto dto) =>
        CreatedResponse(await groupService.AddMemberAsync(groupId, dto));

    [HttpGet("{groupId:long}/members")]
    [Authorize(Policy = "RESEARCH_GROUP_READ")]
    public async Task<ActionResult<BaseResponse<List<ResearchGroupMemberDto>>>> GetMembers(long groupId) =>
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
    public async Task<ActionResult<BaseResponse<ResearchGroupMemberDto>>> ChangeMemberRole(long groupId, string userId, [FromBody] ResearchGroupMemberDto dto) =>
        OkResponse(await groupService.ChangeMemberRoleAsync(groupId, userId, dto), "Research group member role updated successfully");

    [HttpPatch("{groupId:long}/members/{userId}/status")]
    [Authorize(Policy = "RESEARCH_GROUP_MEMBER_MANAGE")]
    public async Task<ActionResult<BaseResponse<ResearchGroupMemberDto>>> UpdateMemberStatus(long groupId, string userId, [FromBody] ResearchGroupMemberDto dto) =>
        OkResponse(await groupService.UpdateMemberStatusAsync(groupId, userId, dto), "Research group member status updated successfully");

    [HttpPatch("{groupId:long}/leader/{userId}")]
    [Authorize(Policy = "RESEARCH_GROUP_MEMBER_MANAGE")]
    public async Task<ActionResult<BaseResponse<ResearchGroupMemberDto>>> AssignLeader(long groupId, string userId) =>
        OkResponse(await groupService.AssignGroupLeaderAsync(groupId, userId), "Research group leader assigned successfully");

    [HttpGet("users/{userId}")]
    [Authorize(Policy = "RESEARCH_GROUP_READ")]
    public async Task<ActionResult<BaseResponse<List<ResearchGroupDto>>>> GetGroupsByUser(string userId) =>
        OkResponse(await groupService.GetGroupsByUserAsync(userId), "User research groups retrieved successfully");
}
