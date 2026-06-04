using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Application.DTOs.OpenAlex;

public class OpenAlexSimilarityResult
{
    public string? MatchedOpenAlexId { get; set; }
    public string? MatchedDoi { get; set; }
    public string? MatchedTitle { get; set; }
    public string? MatchedJournal { get; set; }
    public int? PublicationYear { get; set; }
    public int? CitedByCount { get; set; }
    public double TitleSimilarity { get; set; }
    public double AuthorSimilarity { get; set; }
    public double AbstractSimilarity { get; set; }
    public double ReferenceSimilarity { get; set; }
    public double OverallScore { get; set; }
    public SimilarityRiskLevel RiskLevel { get; set; } = SimilarityRiskLevel.LOW;
    public string RawJson { get; set; } = "{}";
}
