using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Configurations;
using PublicationQualitySystem.Entities;
using PublicationQualitySystem.Enums;

namespace PublicationQualitySystem.Repositories;

public class ResearchGroupMemberRepository(ApplicationDbContext db) : IResearchGroupMemberRepository
{
    public Task<ResearchGroupMember?> FindByGroupAndUserAsync(long groupId, string userId) =>
        db.ResearchGroupMembers.Include(m => m.User).Include(m => m.ResearchGroup)
            .FirstOrDefaultAsync(m => m.ResearchGroupId == groupId && m.UserId == userId);

    public Task<ResearchGroupMember?> FindLeaderAsync(long groupId) =>
        db.ResearchGroupMembers.Include(m => m.User)
            .FirstOrDefaultAsync(m => m.ResearchGroupId == groupId && m.Role == MemberRoleInGroup.LEADER);

    public Task<List<ResearchGroupMember>> FindByGroupAsync(long groupId) =>
        db.ResearchGroupMembers.Include(m => m.User).Where(m => m.ResearchGroupId == groupId).ToListAsync();

    public Task<List<ResearchGroupMember>> FindByUserAsync(string userId) =>
        db.ResearchGroupMembers.Include(m => m.User).Include(m => m.ResearchGroup).Where(m => m.UserId == userId).ToListAsync();
}
