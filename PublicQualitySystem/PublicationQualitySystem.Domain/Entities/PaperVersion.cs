using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Domain.Entities;

public class PaperVersion : BaseEntity
{
    public long PaperId { get; set; }
    public Paper Paper { get; set; } = null!;
    public long? UploadedFileId { get; set; }
    public UploadedFile? UploadedFile { get; set; }
    public int VersionNumber { get; set; }
    public string? VersionName { get; set; }
    public string? ChangeLog { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string? FileType { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? UploadedBy { get; set; }
    public User? UploadedByUser { get; set; }
}
