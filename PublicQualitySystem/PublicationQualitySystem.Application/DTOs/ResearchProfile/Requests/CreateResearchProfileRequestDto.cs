using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Application.DTOs.ResearchProfile.Requests;

public class CreateResearchProfileRequestDto
{
    public string UserId { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Institution { get; set; }
    public string? Specialization { get; set; }
    public string? Orcid { get; set; }
    public string? ResearchInterests { get; set; }
    public AcademicRank? AcademicRank { get; set; }
    public MemberStatus? Status { get; set; }
}
