using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Application.DTOs.ResearchGroup.Requests;

public class ChangeMemberRoleRequestDto
{
    public MemberRoleInGroup? Role { get; set; }
}
