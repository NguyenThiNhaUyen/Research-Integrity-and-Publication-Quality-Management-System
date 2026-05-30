namespace PublicationQualitySystem.Application.DTOs.File;

using PublicationQualitySystem.Domain.Enums;

public class UploadedFileResponse
{
    public long FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string S3Bucket { get; set; } = string.Empty;
    public string S3Key { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long Size { get; set; }
    public long? PaperId { get; set; }
    public string? PaperCode { get; set; }
    public string? Title { get; set; }
    public string? AbstractText { get; set; }
    public string? Keywords { get; set; }
    public string? ResearchField { get; set; }
    public int? CurrentVersion { get; set; }
    public SubmissionStatus? SubmissionStatus { get; set; }
}
