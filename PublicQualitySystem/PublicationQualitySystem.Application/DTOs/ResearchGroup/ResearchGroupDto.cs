using System.ComponentModel.DataAnnotations;

namespace PublicationQualitySystem.Application.DTOs.ResearchGroup;

public class ResearchGroupDto
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Research group name cannot be empty")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? ResearchTopics { get; set; }
    public string? ActiveProjects { get; set; }
    public string? Specialization { get; set; }
    public string? Institution { get; set; }
    public int? TotalPublications { get; set; }
    public int? AcceptedPublications { get; set; }
    public double? AcceptanceRate { get; set; }
    public int? MemberCount { get; set; }
    public ResearchGroupMemberDto? Leader { get; set; }
    public List<ResearchGroupMemberDto>? Members { get; set; }
}
