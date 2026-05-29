using System.ComponentModel.DataAnnotations;
using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Application.DTOs.ResearchGroup;

public class ResearchGroupMemberRequest
{
    [Required(ErrorMessage = "User id cannot be empty")]
    public string? UserId { get; set; }

    public MemberRoleInGroup? Role { get; set; }
    public MemberStatus? Status { get; set; }
    public string? Responsibilities { get; set; }
}
