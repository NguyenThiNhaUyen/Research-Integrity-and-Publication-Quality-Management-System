using System.ComponentModel.DataAnnotations;

namespace PublicationQualitySystem.DTOs.Role;

public class RoleDto
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Role name cannot be empty")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public ISet<string>? Permissions { get; set; }
}
