using System.ComponentModel.DataAnnotations;

namespace PublicationQualitySystem.Application.DTOs.Role;

public class CreateRoleRequest
{
    [Required(ErrorMessage = "Role name cannot be empty")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public ISet<string>? Permissions { get; set; }
}
