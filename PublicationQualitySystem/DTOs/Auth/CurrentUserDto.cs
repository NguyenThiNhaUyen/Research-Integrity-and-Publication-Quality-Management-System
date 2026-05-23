namespace PublicationQualitySystem.DTOs.Auth;

public class CurrentUserDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Sub { get; set; }
    public List<string> Authorities { get; set; } = new();
}
