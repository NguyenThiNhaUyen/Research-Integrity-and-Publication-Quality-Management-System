using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Application.DTOs.OpenAlex;

public class SimilarityResultResponse
{
    public long PaperId { get; set; }
    public long PaperVersionId { get; set; }
    public long PaperMetadataId { get; set; }
    public string Source { get; set; } = string.Empty;
    public SimilarityCheckStatus Status { get; set; }
    public double SimilarityScore { get; set; }
    public SimilarityRiskLevel RiskLevel { get; set; }
    public string? SkipReason { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? CheckedAt { get; set; }
    public IReadOnlyList<SimilarityMatchedPaperResponse> MatchedPapers { get; set; } = Array.Empty<SimilarityMatchedPaperResponse>();
}

public class SimilarityMatchedPaperResponse
{
    public string? OpenAlexId { get; set; }
    public string? Doi { get; set; }
    public string? Title { get; set; }
    public double? TitleSimilarity { get; set; }
    public double? AuthorSimilarity { get; set; }
    public double? AbstractSimilarity { get; set; }
    public double? ReferenceSimilarity { get; set; }
    public double? OverallScore { get; set; }
}
