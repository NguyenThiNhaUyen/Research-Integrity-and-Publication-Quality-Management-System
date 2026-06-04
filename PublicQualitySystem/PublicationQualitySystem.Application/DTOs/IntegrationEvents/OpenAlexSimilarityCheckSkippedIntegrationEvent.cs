namespace PublicationQualitySystem.Application.DTOs.IntegrationEvents;

public class OpenAlexSimilarityCheckSkippedIntegrationEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public long PaperId { get; set; }
    public long PaperVersionId { get; set; }
    public long PaperMetadataId { get; set; }
    public string Reason { get; set; } = "Metadata quality gate failed";
    public int TotalScore { get; set; }
    public int CoreScore { get; set; }
    public string Grade { get; set; } = string.Empty;
    public string? MissingFieldsJson { get; set; }
    public string? WarningsJson { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
