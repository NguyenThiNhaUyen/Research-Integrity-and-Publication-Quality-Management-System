namespace PublicationQualitySystem.Infrastructure.Options;

public class OpenAlexOptions
{
    public string BaseUrl { get; set; } = "https://api.openalex.org";
    public string? ApiKey { get; set; }
    public int MaxCandidates { get; set; } = 10;
    public int TimeoutSeconds { get; set; } = 20;
}
