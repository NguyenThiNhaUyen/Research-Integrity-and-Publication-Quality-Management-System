using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Application.DTOs.Doi;

public class PaperDoiCheckResponse
{
    public long PaperId { get; set; }
    public long PaperMetadataId { get; set; }
    public long? PaperVersionId { get; set; }
    public string? MainDoi { get; set; }
    public DoiValidationStatus MainDoiStatus { get; set; }
    public double? MainDoiTitleSimilarity { get; set; }
    public bool? MainDoiYearMatched { get; set; }
    public string? MainDoiMatchedTitle { get; set; }
    public string? MainDoiMatchedPublisher { get; set; }
    public string? MainDoiIssueCode { get; set; }
    public string? MainDoiIssueMessage { get; set; }
    public int TotalReferences { get; set; }
    public int ReferencesWithDoi { get; set; }
    public int ReferencesMissingDoi { get; set; }
    public int ValidReferenceDois { get; set; }
    public int InvalidReferenceDois { get; set; }
    public int ReferenceDoiTitleMismatches { get; set; }
    public double ReferenceDoiCoveragePercent { get; set; }
    public int MissingReferenceAuthors { get; set; }
    public int MissingReferenceVenues { get; set; }
    public int LowConfidenceReferences { get; set; }
    public int DuplicateReferences { get; set; }
    public int ReferenceCleanlinessIssues { get; set; }
    public int ReferenceQualityScore { get; set; }
    public int OverallScore { get; set; }
    public DoiRiskLevel RiskLevel { get; set; }
    public DoiCheckStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? CheckedAt { get; set; }
    public IReadOnlyList<PaperReferenceDoiCheckResponse> ReferenceChecks { get; set; } = Array.Empty<PaperReferenceDoiCheckResponse>();
}
