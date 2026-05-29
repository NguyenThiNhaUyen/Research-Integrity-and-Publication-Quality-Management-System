using PublicationQualitySystem.Shared.Common;
using PublicationQualitySystem.Application.DTOs.ResearchGroup.Requests;
using PublicationQualitySystem.Application.DTOs.ResearchGroup.Responses;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IResearchGroupService : IBaseCrudService<ResearchGroupResponseDto, CreateResearchGroupRequestDto, UpdateResearchGroupRequestDto>
{
    Task<ResearchGroupMemberResponseDto> AddMemberAsync(long groupId, AddResearchGroupMemberRequestDto dto);
    Task RemoveMemberAsync(long groupId, string userId);
    Task<ResearchGroupMemberResponseDto> ChangeMemberRoleAsync(long groupId, string userId, ChangeMemberRoleRequestDto dto);
    Task<ResearchGroupMemberResponseDto> UpdateMemberStatusAsync(long groupId, string userId, UpdateMemberStatusRequestDto dto);
    Task<ResearchGroupMemberResponseDto> AssignGroupLeaderAsync(long groupId, string userId);
    Task<List<ResearchGroupMemberResponseDto>> GetGroupMembersAsync(long groupId);
    Task<List<ResearchGroupResponseDto>> GetGroupsByUserAsync(string userId);
}
