namespace PublicationQualitySystem.Application.DTOs.Paper;

public class UpdatePaperVersionRequest
{
    public string? VersionName { get; set; }
    public string? ChangeLog { get; set; }
}
