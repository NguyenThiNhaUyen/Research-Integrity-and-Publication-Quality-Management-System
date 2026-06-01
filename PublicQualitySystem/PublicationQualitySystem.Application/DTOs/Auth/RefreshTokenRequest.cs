using System.ComponentModel.DataAnnotations;

namespace PublicationQualitySystem.Application.DTOs.Auth;

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token cannot be empty")]
    public string RefreshToken { get; set; } = string.Empty;
}
