using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Api.Common;
using PublicationQualitySystem.Shared.Common;
using PublicationQualitySystem.Application.DTOs.ResearchGroup.Requests;
using PublicationQualitySystem.Application.DTOs.ResearchGroup.Responses;
using PublicationQualitySystem.Application.Services.Interfaces;

namespace PublicationQualitySystem.Api.Controllers;

[Route("api/lab-members/research-groups")]
public class ResearchGroupController(IResearchGroupService groupService) : ApiBaseController
{
    [HttpPost]
    public async Task<ActionResult<BaseResponse<ResearchGroupResponseDto>>> Create([FromBody] CreateResearchGroupRequestDto dto) => CreatedResponse(await groupService.CreateAsync(dto));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<BaseResponse<ResearchGroupResponseDto>>> GetById(long id) => OkResponse(await groupService.GetByIdAsync(id), "Get by id successfully");

    [HttpPut("{id:long}")]
    public async Task<ActionResult<BaseResponse<ResearchGroupResponseDto>>> Update(long id, [FromBody] UpdateResearchGroupRequestDto dto) => OkResponse(await groupService.UpdateAsync(id, dto), "Update successfully");

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<BaseResponse<object>>> Delete(long id)
    {
        await groupService.DeleteAsync(id);
        return OkResponse<object>(null, "Delete successfully");
    }

    [HttpGet]
    [Authorize(Policy = "RESEARCH_GROUP_READ")]
    public async Task<ActionResult<BaseResponse<List<ResearchGroupResponseDto>>>> GetAll([FromQuery] int page = 0, [FromQuery] int size = 20) =>
        OkResponse(await groupService.GetAllAsync(page, size), "Research groups retrieved successfully");

    [HttpPost("{groupId:long}/members")]
    [Authorize(Policy = "RESEARCH_GROUP_MEMBER_MANAGE")]
    public async Task<ActionResult<BaseResponse<ResearchGroupMemberResponseDto>>> AddMember(long groupId, [FromBody] AddResearchGroupMemberRequestDto dto) =>
        CreatedResponse(await groupService.AddMemberAsync(groupId, dto));

    [HttpGet("{groupId:long}/members")]
    [Authorize(Policy = "RESEARCH_GROUP_READ")]
    public async Task<ActionResult<BaseResponse<List<ResearchGroupMemberResponseDto>>>> GetMembers(long groupId) =>
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
    public async Task<ActionResult<BaseResponse<ResearchGroupMemberResponseDto>>> ChangeMemberRole(long groupId, string userId, [FromBody] ChangeMemberRoleRequestDto dto) =>
        OkResponse(await groupService.ChangeMemberRoleAsync(groupId, userId, dto), "Research group member role updated successfully");

    [HttpPatch("{groupId:long}/members/{userId}/status")]
    [Authorize(Policy = "RESEARCH_GROUP_MEMBER_MANAGE")]
    public async Task<ActionResult<BaseResponse<ResearchGroupMemberResponseDto>>> UpdateMemberStatus(long groupId, string userId, [FromBody] UpdateMemberStatusRequestDto dto) =>
        OkResponse(await groupService.UpdateMemberStatusAsync(groupId, userId, dto), "Research group member status updated successfully");

    [HttpPatch("{groupId:long}/leader/{userId}")]
    [Authorize(Policy = "RESEARCH_GROUP_MEMBER_MANAGE")]
    public async Task<ActionResult<BaseResponse<ResearchGroupMemberResponseDto>>> AssignLeader(long groupId, string userId) =>
        OkResponse(await groupService.AssignGroupLeaderAsync(groupId, userId), "Research group leader assigned successfully");

    [HttpGet("users/{userId}")]
    [Authorize(Policy = "RESEARCH_GROUP_READ")]
    public async Task<ActionResult<BaseResponse<List<ResearchGroupResponseDto>>>> GetGroupsByUser(string userId) =>
        OkResponse(await groupService.GetGroupsByUserAsync(userId), "User research groups retrieved successfully");
}
