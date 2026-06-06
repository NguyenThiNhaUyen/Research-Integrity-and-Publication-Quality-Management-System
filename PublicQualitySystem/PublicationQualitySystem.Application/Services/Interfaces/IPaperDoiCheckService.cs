using PublicationQualitySystem.Application.DTOs.Doi;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IPaperDoiCheckService
{
    Task<PaperDoiCheckResponse> RunAsync(
        long paperId,
        long paperMetadataId,
        long? paperVersionId = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<PaperDoiCheckResponse> GetByPaperIdAsync(
        long paperId,
        CancellationToken cancellationToken = default);

    Task<PaperDoiCheckSummaryResponse> GetSummaryByPaperIdAsync(
        long paperId,
        CancellationToken cancellationToken = default);
}
