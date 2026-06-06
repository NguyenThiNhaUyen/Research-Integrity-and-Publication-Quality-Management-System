using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Domain.Entities;

public class PaperDoiCheck : BaseEntity
{
    public long PaperId { get; set; }
    public Paper Paper { get; set; } = null!;
    public long PaperMetadataId { get; set; }
    public PaperMetadata PaperMetadata { get; set; } = null!;
    public long? PaperVersionId { get; set; }
    public PaperVersion? PaperVersion { get; set; }
    public string? MainDoi { get; set; }
    public DoiValidationStatus MainDoiStatus { get; set; } = DoiValidationStatus.MISSING;
    public double? MainDoiTitleSimilarity { get; set; }
    public bool? MainDoiYearMatched { get; set; }
    public string? MainDoiMatchedTitle { get; set; }
    public string? MainDoiMatchedPublisher { get; set; }
    public string? MainDoiIssueCode { get; set; }
    public string? MainDoiIssueMessage { get; set; }
    public string? MainDoiRawJson { get; set; }
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
    public DoiRiskLevel RiskLevel { get; set; } = DoiRiskLevel.LOW;
    public DoiCheckStatus Status { get; set; } = DoiCheckStatus.PENDING;
    public string? ErrorMessage { get; set; }
    public string RawJson { get; set; } = "{}";
    public DateTime? CheckedAt { get; set; }
    public ICollection<PaperReferenceDoiCheck> ReferenceChecks { get; set; } = new List<PaperReferenceDoiCheck>();
}
