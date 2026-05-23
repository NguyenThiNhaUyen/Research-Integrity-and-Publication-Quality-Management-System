namespace PublicationQualitySystem.DTOs.Auth;

public class AuthResponseDto
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? IdToken { get; set; }
    public string? TokenType { get; set; }
    public int? ExpiresIn { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
}
