using PublicationQualitySystem.Application.DTOs.Grobid;

namespace PublicationQualitySystem.Application.DTOs.References;

public class ReferenceNormalizationResult
{
    public ReferenceDto Reference { get; set; } = new();
    public double ParseConfidenceScore { get; set; }
    public IReadOnlyList<string> IssueCodes { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> WarningMessages { get; set; } = Array.Empty<string>();
}
