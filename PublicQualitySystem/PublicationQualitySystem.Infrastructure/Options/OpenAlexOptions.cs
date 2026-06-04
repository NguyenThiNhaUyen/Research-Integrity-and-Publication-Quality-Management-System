namespace PublicationQualitySystem.Infrastructure.Options;

public class OpenAlexOptions
{
    public string BaseUrl { get; set; } = "https://api.openalex.org";
    public string? ApiKey { get; set; }
    public int MaxCandidates { get; set; } = 10;
    public int TimeoutSeconds { get; set; } = 20;
    public int MaxReferenceResolution { get; set; } = 30;
    public int ReferenceTitleThreshold { get; set; } = 85;
    public int AuthorNameThreshold { get; set; } = 75;
}
