using PublicationQualitySystem.Shared.Common;
using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Domain.Entities;

public class Paper : BaseEntity
{
    public string PaperCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? AbstractText { get; set; }
    public string? Keywords { get; set; }
    public string? ResearchField { get; set; }
    public string? FileUrl { get; set; }
    public string? FileType { get; set; }
    public int CurrentVersion { get; set; } = 1;
    public SubmissionStatus SubmissionStatus { get; set; } = SubmissionStatus.DRAFT;
    public ICollection<PaperAuthor> Authors { get; set; } = new List<PaperAuthor>();
    public ICollection<PaperVersion> Versions { get; set; } = new List<PaperVersion>();
}
