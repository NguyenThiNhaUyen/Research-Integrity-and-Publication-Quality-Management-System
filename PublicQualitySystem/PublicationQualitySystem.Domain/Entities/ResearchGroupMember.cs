using PublicationQualitySystem.Shared.Common;
using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Domain.Entities;

public class ResearchGroupMember : BaseEntity
{
    public long ResearchGroupId { get; set; }
    public ResearchGroup ResearchGroup { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;
    public MemberRoleInGroup Role { get; set; } = MemberRoleInGroup.MEMBER;
    public MemberStatus Status { get; set; } = MemberStatus.ACTIVE;
    public DateOnly? JoinedAt { get; set; }
    public DateOnly? LeftAt { get; set; }
    public double ContributionScore { get; set; }
    public int AssignedReviews { get; set; }
    public int CompletedReviews { get; set; }
    public string? Responsibilities { get; set; }
}
