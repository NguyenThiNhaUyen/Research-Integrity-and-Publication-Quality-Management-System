using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Application.DTOs.Paper;

public class PaperUploadProcessingResult
{
    public long PaperId { get; set; }
    public string PaperCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? AbstractText { get; set; }
    public string? Keywords { get; set; }
    public string? ResearchField { get; set; }
    public int CurrentVersion { get; set; }
    public SubmissionStatus SubmissionStatus { get; set; }
}
