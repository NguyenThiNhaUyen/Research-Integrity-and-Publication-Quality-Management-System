using PublicationQualitySystem.Application.DTOs.PaperReviewProfile;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IPaperReviewProfileService
{
    Task<PaperReviewProfileResponse> CreateOrUpdateAsync(long paperId, CreatePaperReviewProfileRequest request);
    Task<PaperReviewProfileResponse> GetByPaperIdAsync(long paperId);
}
