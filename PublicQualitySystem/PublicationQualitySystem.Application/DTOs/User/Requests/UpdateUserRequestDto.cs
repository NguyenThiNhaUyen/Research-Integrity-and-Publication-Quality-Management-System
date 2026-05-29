using System.ComponentModel.DataAnnotations;

namespace PublicationQualitySystem.Application.DTOs.User.Requests;

public class UpdateUserRequestDto
{
    [Required(ErrorMessage = "Full name cannot be empty")]
    [MaxLength(255, ErrorMessage = "Full name cannot exceed 255 characters")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email cannot be empty")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
}
