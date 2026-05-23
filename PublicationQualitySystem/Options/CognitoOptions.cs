namespace PublicationQualitySystem.Options;

public class CognitoOptions
{
    public string? Region { get; set; }
    public string? UserPoolId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string AdminGroupName { get; set; } = "ADMIN";

    public string? Issuer =>
        string.IsNullOrWhiteSpace(Region) || string.IsNullOrWhiteSpace(UserPoolId)
            ? null
            : $"https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}";
}
