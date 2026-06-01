namespace PublicationQualitySystem.Application.DTOs.Auth;

public class CurrentUserResponse
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Sub { get; set; }
    public List<string> Authorities { get; set; } = new();
}
