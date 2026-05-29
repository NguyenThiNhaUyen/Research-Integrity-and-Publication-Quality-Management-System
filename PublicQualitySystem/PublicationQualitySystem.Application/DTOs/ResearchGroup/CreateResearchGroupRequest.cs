using System.ComponentModel.DataAnnotations;

namespace PublicationQualitySystem.Application.DTOs.ResearchGroup;

public class CreateResearchGroupRequest
{
    [Required(ErrorMessage = "Research group name cannot be empty")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? ResearchTopics { get; set; }
    public string? ActiveProjects { get; set; }
    public string? Specialization { get; set; }
    public string? Institution { get; set; }
}
