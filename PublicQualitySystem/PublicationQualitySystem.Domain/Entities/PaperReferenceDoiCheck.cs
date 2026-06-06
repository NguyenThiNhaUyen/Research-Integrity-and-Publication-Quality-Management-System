using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Shared.Common;

namespace PublicationQualitySystem.Domain.Entities;

public class PaperReferenceDoiCheck : BaseEntity
{
    public long PaperDoiCheckId { get; set; }
    public PaperDoiCheck PaperDoiCheck { get; set; } = null!;
    public int ReferenceOrdinal { get; set; }
    public string? RawText { get; set; }
    public string? ExtractedDoi { get; set; }
    public string? ExtractedTitle { get; set; }
    public int? ExtractedYear { get; set; }
    public string? NormalizedTitle { get; set; }
    public string? NormalizedDoi { get; set; }
    public string? NormalizedJournal { get; set; }
    public int? NormalizedYear { get; set; }
    public bool DoiFormatValid { get; set; }
    public string? ValidationSource { get; set; }
    public DoiValidationStatus ValidationStatus { get; set; } = DoiValidationStatus.MISSING;
    public string? MatchedDoi { get; set; }
    public string? MatchedTitle { get; set; }
    public string? MatchedPublisher { get; set; }
    public int? MatchedYear { get; set; }
    public double? TitleSimilarity { get; set; }
    public bool? YearMatched { get; set; }
    public string? IssueCode { get; set; }
    public string? IssueMessage { get; set; }
    public int MetadataCompletenessScore { get; set; }
    public double ParseConfidenceScore { get; set; }
    public string IssueCodesJson { get; set; } = "[]";
    public string RawJson { get; set; } = "{}";
    public DateTime? CheckedAt { get; set; }
}
