using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Application.DTOs.PaperReviewProfile;

public class CreatePaperReviewProfileRequest
{
    public SubmissionTargetType TargetType { get; set; }
    public ResearchField ResearchField { get; set; }
    public PaperType PaperType { get; set; }
    public ReviewGoal ReviewGoal { get; set; }
    public string? Note { get; set; }
}
