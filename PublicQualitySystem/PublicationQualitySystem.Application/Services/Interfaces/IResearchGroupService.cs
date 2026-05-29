using PublicationQualitySystem.Shared.Common;
using PublicationQualitySystem.Application.DTOs.Auth;
using PublicationQualitySystem.Application.DTOs.File;
using PublicationQualitySystem.Application.DTOs.ResearchGroup;
using PublicationQualitySystem.Application.DTOs.ResearchProfile;
using PublicationQualitySystem.Application.DTOs.Role;
using PublicationQualitySystem.Application.DTOs.User;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IResearchGroupService : IBaseCrudService<CreateResearchGroupRequest, UpdateResearchGroupRequest, ResearchGroupResponse, long>
{
    Task<List<ResearchGroupResponse>> GetAllAsync(int page, int size);
    Task<ResearchGroupMemberResponse> AddMemberAsync(long groupId, ResearchGroupMemberRequest dto);
    Task RemoveMemberAsync(long groupId, string userId);
    Task<ResearchGroupMemberResponse> ChangeMemberRoleAsync(long groupId, string userId, ResearchGroupMemberRequest dto);
    Task<ResearchGroupMemberResponse> UpdateMemberStatusAsync(long groupId, string userId, ResearchGroupMemberRequest dto);
    Task<ResearchGroupMemberResponse> AssignGroupLeaderAsync(long groupId, string userId);
    Task<List<ResearchGroupMemberResponse>> GetGroupMembersAsync(long groupId);
    Task<List<ResearchGroupResponse>> GetGroupsByUserAsync(string userId);
}
