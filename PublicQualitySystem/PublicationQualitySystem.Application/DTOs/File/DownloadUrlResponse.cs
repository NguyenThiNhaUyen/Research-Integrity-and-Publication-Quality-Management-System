namespace PublicationQualitySystem.Application.DTOs.File;

public class DownloadUrlResponse
{
    public long FileId { get; set; }
    public string FileKey { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
