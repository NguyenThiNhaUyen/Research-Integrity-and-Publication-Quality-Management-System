using PublicationQualitySystem.Application.DTOs.Grobid;
using PublicationQualitySystem.Application.DTOs.References;

namespace PublicationQualitySystem.Application.DTOs.Metadata;

public class MetadataNormalizationResult
{
    public GrobidMetadataResponse Metadata { get; set; } = new();
    public int MainMetadataCleanlinessScore { get; set; }
    public int ReferenceCleanlinessScore { get; set; }
    public int DirtyFieldCount { get; set; }
    public IReadOnlyList<string> IssueCodes { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> WarningMessages { get; set; } = Array.Empty<string>();
    public IReadOnlyList<ReferenceQualityResult> ReferenceResults { get; set; } = Array.Empty<ReferenceQualityResult>();
}
