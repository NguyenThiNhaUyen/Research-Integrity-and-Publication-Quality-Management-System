using PublicationQualitySystem.Shared.Common;
using PublicationQualitySystem.Application.DTOs.Auth;
using PublicationQualitySystem.Application.DTOs.File;
using PublicationQualitySystem.Application.DTOs.ResearchGroup;
using PublicationQualitySystem.Application.DTOs.ResearchProfile;
using PublicationQualitySystem.Application.DTOs.Role;
using PublicationQualitySystem.Application.DTOs.User;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IResearchGroupService : IBaseCrudService<ResearchGroupDto>
{
    Task<ResearchGroupMemberDto> AddMemberAsync(long groupId, ResearchGroupMemberDto dto);
    Task RemoveMemberAsync(long groupId, string userId);
    Task<ResearchGroupMemberDto> ChangeMemberRoleAsync(long groupId, string userId, ResearchGroupMemberDto dto);
    Task<ResearchGroupMemberDto> UpdateMemberStatusAsync(long groupId, string userId, ResearchGroupMemberDto dto);
    Task<ResearchGroupMemberDto> AssignGroupLeaderAsync(long groupId, string userId);
    Task<List<ResearchGroupMemberDto>> GetGroupMembersAsync(long groupId);
    Task<List<ResearchGroupDto>> GetGroupsByUserAsync(string userId);
}
