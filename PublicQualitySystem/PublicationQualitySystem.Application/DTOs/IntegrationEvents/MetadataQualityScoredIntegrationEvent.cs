namespace PublicationQualitySystem.Application.DTOs.IntegrationEvents;

public class MetadataQualityScoredIntegrationEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public long PaperId { get; set; }
    public long PaperVersionId { get; set; }
    public long PaperMetadataId { get; set; }
    public int TotalScore { get; set; }
    public int CoreScore { get; set; }
    public string Grade { get; set; } = string.Empty;
    public bool CanProceed { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
