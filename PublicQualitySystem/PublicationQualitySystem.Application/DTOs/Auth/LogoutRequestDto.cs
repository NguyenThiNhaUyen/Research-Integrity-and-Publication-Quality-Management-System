using System.ComponentModel.DataAnnotations;

namespace PublicationQualitySystem.Application.DTOs.Auth;

public class LogoutRequestDto
{
    [Required(ErrorMessage = "Access token cannot be empty")]
    public string AccessToken { get; set; } = string.Empty;
}
