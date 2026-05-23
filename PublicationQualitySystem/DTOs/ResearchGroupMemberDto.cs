using System.ComponentModel.DataAnnotations;
using PublicationQualitySystem.Enums;

namespace PublicationQualitySystem.DTOs;

public class ResearchGroupMemberDto
{
    public long Id { get; set; }

    [Required(ErrorMessage = "User id cannot be empty")]
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
