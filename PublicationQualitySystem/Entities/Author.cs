using PublicationQualitySystem.Common;

namespace PublicationQualitySystem.Entities;

public class Author : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Orcid { get; set; }
    public string? Affiliation { get; set; }
    public ICollection<PaperAuthor> PaperAuthors { get; set; } = new HashSet<PaperAuthor>();
}
