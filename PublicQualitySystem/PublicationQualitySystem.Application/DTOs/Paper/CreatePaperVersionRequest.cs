namespace PublicationQualitySystem.Application.DTOs.Paper;

public class CreatePaperVersionRequest
{
    public long FileId { get; set; }
    public string? VersionName { get; set; }
    public string? ChangeLog { get; set; }
}
