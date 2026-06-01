using System.ComponentModel.DataAnnotations;

namespace PublicationQualitySystem.Application.DTOs.User;

public class CreateUserRequest
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Full name cannot be empty")]
    [MaxLength(255, ErrorMessage = "Full name cannot exceed 255 characters")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email cannot be empty")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    public string? Password { get; set; }
}
