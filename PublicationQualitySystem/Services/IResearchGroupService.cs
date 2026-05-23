using PublicationQualitySystem.Common;
using PublicationQualitySystem.DTOs;

namespace PublicationQualitySystem.Services;

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
