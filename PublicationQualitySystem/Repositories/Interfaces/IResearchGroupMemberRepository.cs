using PublicationQualitySystem.Entities;

namespace PublicationQualitySystem.Repositories.Interfaces;

public interface IResearchGroupMemberRepository
{
    Task<ResearchGroupMember?> FindByGroupAndUserAsync(long groupId, string userId);
    Task<ResearchGroupMember?> FindLeaderAsync(long groupId);
    Task<List<ResearchGroupMember>> FindByGroupAsync(long groupId);
    Task<List<ResearchGroupMember>> FindByUserAsync(string userId);
}
