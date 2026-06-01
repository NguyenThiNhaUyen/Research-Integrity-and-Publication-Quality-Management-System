using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Domain.Entities;

public class UploadedFile : BaseEntity
{
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileKey { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public UploadType UploadType { get; set; } = UploadType.OTHER;
    public string? UploadedBy { get; set; }
    public User? UploadedByUser { get; set; }
    public ICollection<PaperVersion> PaperVersions { get; set; } = new List<PaperVersion>();
}
