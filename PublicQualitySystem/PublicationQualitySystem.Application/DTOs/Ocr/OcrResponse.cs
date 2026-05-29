namespace PublicationQualitySystem.Application.DTOs.Ocr;

public class OcrResponse
{
    public bool Success { get; set; }
    public long FileId { get; set; }
    public string ObjectKey { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
