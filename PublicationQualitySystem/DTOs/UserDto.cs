using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PublicationQualitySystem.DTOs;

public class UserDto
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Full name cannot be empty")]
    [MaxLength(255, ErrorMessage = "Full name cannot exceed 255 characters")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email cannot be empty")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Password { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ISet<RoleDto>? Roles { get; set; }
}
