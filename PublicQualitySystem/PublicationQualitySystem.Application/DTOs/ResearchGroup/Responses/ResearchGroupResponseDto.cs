namespace PublicationQualitySystem.Application.DTOs.ResearchGroup.Responses;

public class ResearchGroupResponseDto
{
    public long Id { get; set; }
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
    public ResearchGroupMemberResponseDto? Leader { get; set; }
    public List<ResearchGroupMemberResponseDto>? Members { get; set; }
}
