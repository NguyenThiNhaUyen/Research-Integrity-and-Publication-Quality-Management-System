namespace PublicationQualitySystem.Application.DTOs.IntegrationEvents;

public class OpenAlexSimilarityCheckRequestedIntegrationEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public long PaperId { get; set; }
    public long PaperVersionId { get; set; }
    public long PaperMetadataId { get; set; }
    public string TriggerReason { get; set; } = "Metadata quality gate passed";
    public string? CorrelationId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
