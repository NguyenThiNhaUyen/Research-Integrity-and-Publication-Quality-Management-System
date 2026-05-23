using System.ComponentModel.DataAnnotations;

namespace PublicationQualitySystem.DTOs;

public class RefreshTokenRequestDto
{
    [Required(ErrorMessage = "Refresh token cannot be empty")]
    public string RefreshToken { get; set; } = string.Empty;
}
