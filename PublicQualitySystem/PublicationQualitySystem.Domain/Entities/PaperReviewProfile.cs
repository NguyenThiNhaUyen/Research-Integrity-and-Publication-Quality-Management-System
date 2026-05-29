using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Domain.Entities;

public class PaperReviewProfile : BaseEntity
{
    public long PaperId { get; set; }
    public Paper Paper { get; set; } = null!;
    public SubmissionTargetType TargetType { get; set; }
    public ResearchField ResearchField { get; set; }
    public PaperType PaperType { get; set; }
    public ReviewGoal ReviewGoal { get; set; }
    public string? Note { get; set; }
}
