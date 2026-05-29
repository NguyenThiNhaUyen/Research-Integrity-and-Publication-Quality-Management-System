namespace PublicationQualitySystem.Application.DTOs.ResearchGroup.Requests;

public class UpdateResearchGroupRequestDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ResearchTopics { get; set; }
    public string? ActiveProjects { get; set; }
    public string? Specialization { get; set; }
    public string? Institution { get; set; }
    public int? TotalPublications { get; set; }
    public int? AcceptedPublications { get; set; }
    public double? AcceptanceRate { get; set; }
}
