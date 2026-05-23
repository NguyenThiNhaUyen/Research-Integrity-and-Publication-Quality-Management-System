namespace PublicationQualitySystem.Options;

public class AwsOptions
{
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? Region { get; set; }
    public CognitoOptions Cognito { get; set; } = new();
    public S3Options S3 { get; set; } = new();
}
