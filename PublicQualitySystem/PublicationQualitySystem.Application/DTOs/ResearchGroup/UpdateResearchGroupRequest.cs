namespace PublicationQualitySystem.Application.DTOs.ResearchGroup;

public class UpdateResearchGroupRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ResearchTopics { get; set; }
    public string? ActiveProjects { get; set; }
    public string? Specialization { get; set; }
    public string? Institution { get; set; }
}
