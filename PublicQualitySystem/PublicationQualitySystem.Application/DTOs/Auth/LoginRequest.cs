using System.ComponentModel.DataAnnotations;

namespace PublicationQualitySystem.Application.DTOs.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "Email cannot be empty")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password cannot be empty")]
    public string Password { get; set; } = string.Empty;
}
