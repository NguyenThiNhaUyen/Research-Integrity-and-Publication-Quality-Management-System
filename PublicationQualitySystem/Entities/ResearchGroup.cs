using PublicationQualitySystem.Common;

namespace PublicationQualitySystem.Entities;

public class ResearchGroup : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ResearchTopics { get; set; }
    public string? ActiveProjects { get; set; }
    public string? Specialization { get; set; }
    public string? Institution { get; set; }
    public int TotalPublications { get; set; }
    public int AcceptedPublications { get; set; }
    public double AcceptanceRate { get; set; }
    public ICollection<ResearchGroupMember> Memberships { get; set; } = new HashSet<ResearchGroupMember>();
}
