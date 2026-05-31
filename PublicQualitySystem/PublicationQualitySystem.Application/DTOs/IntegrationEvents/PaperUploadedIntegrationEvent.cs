namespace PublicationQualitySystem.Application.DTOs.IntegrationEvents;

public class PaperUploadedIntegrationEvent
{
    public long PaperId { get; set; }
    public long PaperVersionId { get; set; }
    public string PdfS3Key { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}
