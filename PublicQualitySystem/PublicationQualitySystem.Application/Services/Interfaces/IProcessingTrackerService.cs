using PublicationQualitySystem.Application.DTOs.Processing;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IProcessingTrackerService
{
    Task<PaperProcessingTrackerResponse> GetByPaperVersionIdAsync(long paperVersionId, CancellationToken cancellationToken);
}
