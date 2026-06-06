using PublicationQualitySystem.Application.DTOs.OpenAlex;
using PublicationQualitySystem.Domain.Entities;

namespace PublicationQualitySystem.Application.Mappings;

public static class SimilarityResultMapper
{
    public static SimilarityResultResponse ToResponse(PaperSimilarityCheck check, long paperVersionId)
    {
        var matchedPapers = string.IsNullOrWhiteSpace(check.MatchedOpenAlexId)
            && string.IsNullOrWhiteSpace(check.MatchedDoi)
            && string.IsNullOrWhiteSpace(check.MatchedTitle)
                ? Array.Empty<SimilarityMatchedPaperResponse>()
                :
                [
                    new SimilarityMatchedPaperResponse
                    {
                        OpenAlexId = check.MatchedOpenAlexId,
                        Doi = check.MatchedDoi,
                        Title = check.MatchedTitle,
                        TitleSimilarity = check.TitleSimilarity,
                        AuthorSimilarity = check.AuthorSimilarity,
                        AbstractSimilarity = check.AbstractSimilarity,
                        ReferenceSimilarity = check.ReferenceSimilarity,
                        OverallScore = check.OverallScore
                    }
                ];

        return new SimilarityResultResponse
        {
            PaperId = check.PaperId,
            PaperVersionId = paperVersionId,
            PaperMetadataId = check.PaperMetadataId,
            Source = check.Source,
            Status = check.Status,
            SimilarityScore = check.OverallScore ?? 0,
            RiskLevel = check.RiskLevel,
            SkipReason = check.SkipReason,
            ErrorMessage = check.ErrorMessage,
            CheckedAt = check.CheckedAt,
            MatchedPapers = matchedPapers
        };
    }
}
