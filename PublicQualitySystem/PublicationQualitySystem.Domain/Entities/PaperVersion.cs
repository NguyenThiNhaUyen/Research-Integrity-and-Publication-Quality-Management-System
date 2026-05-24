using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Domain.Entities;

public class PaperVersion : BaseEntity
{
    public long PaperId { get; set; }
    public Paper Paper { get; set; } = null!;
    public int VersionNumber { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string? FileType { get; set; }
}
