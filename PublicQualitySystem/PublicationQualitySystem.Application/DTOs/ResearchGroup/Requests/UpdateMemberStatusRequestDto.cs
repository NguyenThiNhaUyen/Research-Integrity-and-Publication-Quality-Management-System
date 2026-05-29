using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Application.DTOs.ResearchGroup.Requests;

public class UpdateMemberStatusRequestDto
{
    public MemberStatus? Status { get; set; }
}
