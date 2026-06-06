using PublicationQualitySystem.Application.DTOs.Grobid;

namespace PublicationQualitySystem.Application.DTOs.References;

public class ReferenceQualityResult
{
    public ReferenceDto Reference { get; set; } = new();
    public string? NormalizedTitle { get; set; }
    public string? NormalizedDoi { get; set; }
    public string? NormalizedJournal { get; set; }
    public int? NormalizedYear { get; set; }
    public bool DoiFormatValid { get; set; }
    public int MetadataCompletenessScore { get; set; }
    public double ParseConfidenceScore { get; set; }
    public IReadOnlyList<string> IssueCodes { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> WarningMessages { get; set; } = Array.Empty<string>();
}
