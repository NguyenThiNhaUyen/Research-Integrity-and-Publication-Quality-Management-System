namespace PublicationQualitySystem.Infrastructure.Options;

public class ManuscriptUploadOptions
{
    public const long DefaultMaxFileSizeBytes = 25 * 1024 * 1024;

    public long MaxFileSizeBytes { get; set; } = DefaultMaxFileSizeBytes;
    public int DownloadUrlExpirationMinutes { get; set; } = 10;
}
