namespace PublicationQualitySystem.Application.DTOs.Metadata;

public class MetadataQualityScoreResponse
{
    public int TotalScore { get; set; }
    public int CoreScore { get; set; }
    public int ExtendedScore { get; set; }
    public int EnrichmentScore { get; set; }
    public string Grade { get; set; } = string.Empty;
    public bool CanProceed { get; set; }
    public List<string> MissingFields { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public Dictionary<string, int> FieldScores { get; set; } = [];
}
