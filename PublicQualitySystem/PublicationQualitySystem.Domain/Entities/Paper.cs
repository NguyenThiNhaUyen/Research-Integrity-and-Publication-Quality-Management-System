using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Domain.Entities;

public class Paper : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public int CurrentVersion { get; set; }
    public ICollection<PaperVersion> Versions { get; set; } = new HashSet<PaperVersion>();
}
