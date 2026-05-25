namespace PublicationQualitySystem.Application.DTOs.Paper;

public class PaperVersionResponseDto
{
    public long Id { get; set; }
    public long PaperId { get; set; }
    public int VersionNumber { get; set; }
    public string? VersionName { get; set; }
    public string? ChangeLog { get; set; }
    public long? FileId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileKey { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool Deleted { get; set; }
}
