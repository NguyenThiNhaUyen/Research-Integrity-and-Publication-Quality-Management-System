using System.ComponentModel.DataAnnotations;

namespace PublicationQualitySystem.DTOs;

public class LogoutRequestDto
{
    [Required(ErrorMessage = "Access token cannot be empty")]
    public string AccessToken { get; set; } = string.Empty;
}
