using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Application.DTOs.PaperReviewProfile;

public class PaperReviewProfileResponse
{
    public long Id { get; set; }
    public long PaperId { get; set; }
    public SubmissionTargetType TargetType { get; set; }
    public ResearchField ResearchField { get; set; }
    public PaperType PaperType { get; set; }
    public ReviewGoal ReviewGoal { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
