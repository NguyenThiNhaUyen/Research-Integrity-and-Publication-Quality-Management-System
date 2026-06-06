using PublicationQualitySystem.Application.DTOs.Paper;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IPaperVersionService
{
    Task<IReadOnlyList<PaperVersionResponse>> GetVersionsByPaperIdAsync(long paperId, CancellationToken cancellationToken);

    Task<PaperVersionResponse> GetVersionAsync(long paperVersionId, CancellationToken cancellationToken);
}
