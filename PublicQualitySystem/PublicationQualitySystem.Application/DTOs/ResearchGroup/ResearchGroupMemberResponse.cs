using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Application.DTOs.ResearchGroup;

public class ResearchGroupMemberResponse
{
    public long Id { get; set; }
    public string? UserId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public MemberRoleInGroup? Role { get; set; }
    public MemberStatus? Status { get; set; }
    public DateOnly? JoinedAt { get; set; }
    public DateOnly? LeftAt { get; set; }
    public double? ContributionScore { get; set; }
    public int? AssignedReviews { get; set; }
    public int? CompletedReviews { get; set; }
    public string? Responsibilities { get; set; }
}
