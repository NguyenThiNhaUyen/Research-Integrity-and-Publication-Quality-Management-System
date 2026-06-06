namespace PublicationQualitySystem.Application.DTOs.Metadata;

public class MetadataQualityResponse
{
    public long PaperId { get; set; }
    public long PaperVersionId { get; set; }
    public long PaperMetadataId { get; set; }
    public int MetadataScore { get; set; }
    public int CompletenessScore { get; set; }
    public int ConsistencyScore { get; set; }
    public int AuthorityScore { get; set; }
    public string Grade { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public IReadOnlyList<string> MissingFields { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, int> FieldScores { get; set; } = new Dictionary<string, int>();
    public DateTime? ScoredAt { get; set; }
}
