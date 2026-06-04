using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Domain.Entities;

public class PaperSimilarityCheck : BaseEntity
{
    public long PaperId { get; set; }
    public Paper Paper { get; set; } = null!;
    public long PaperMetadataId { get; set; }
    public PaperMetadata PaperMetadata { get; set; } = null!;
    public string Source { get; set; } = "OpenAlex";
    public SimilarityCheckStatus Status { get; set; } = SimilarityCheckStatus.PENDING;
    public string? MatchedOpenAlexId { get; set; }
    public string? MatchedDoi { get; set; }
    public string? MatchedTitle { get; set; }
    public double? TitleSimilarity { get; set; }
    public double? AuthorSimilarity { get; set; }
    public double? AbstractSimilarity { get; set; }
    public double? ReferenceSimilarity { get; set; }
    public double? OverallScore { get; set; }
    public SimilarityRiskLevel RiskLevel { get; set; } = SimilarityRiskLevel.LOW;
    public string? SkipReason { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RawJson { get; set; }
    public DateTime? CheckedAt { get; set; }
}
