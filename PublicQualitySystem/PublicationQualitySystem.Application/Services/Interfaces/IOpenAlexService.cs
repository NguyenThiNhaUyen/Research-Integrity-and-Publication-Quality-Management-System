using PublicationQualitySystem.Application.DTOs.OpenAlex;
using PublicationQualitySystem.Domain.Entities;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IOpenAlexService
{
    Task<SimilarityResultResponse> GetSimilarityByPaperVersionIdAsync(
        long paperVersionId,
        CancellationToken cancellationToken = default);

    Task<OpenAlexWorkDto?> GetWorkByDoiAsync(string doi, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OpenAlexWorkDto>> SearchWorksByTitleAsync(
        string title,
        int maxCandidates,
        CancellationToken cancellationToken = default);

    Task<OpenAlexSimilarityResult?> CheckSimilarityAsync(
        PaperMetadata metadata,
        CancellationToken cancellationToken = default);
}
