using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Application.DTOs.Doi;

public class PaperDoiCheckSummaryResponse
{
    public long PaperId { get; set; }
    public long PaperMetadataId { get; set; }
    public long? PaperVersionId { get; set; }
    public string? MainDoi { get; set; }
    public DoiValidationStatus MainDoiStatus { get; set; }
    public string? MainDoiIssueCode { get; set; }
    public string? MainDoiIssueMessage { get; set; }
    public double ReferenceDoiCoveragePercent { get; set; }
    public int MissingReferenceAuthors { get; set; }
    public int MissingReferenceVenues { get; set; }
    public int LowConfidenceReferences { get; set; }
    public int ReferenceQualityScore { get; set; }
    public int OverallScore { get; set; }
    public DoiRiskLevel RiskLevel { get; set; }
    public DoiCheckStatus Status { get; set; }
    public DateTime? CheckedAt { get; set; }
}
