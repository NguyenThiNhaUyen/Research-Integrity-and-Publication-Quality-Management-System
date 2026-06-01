namespace PublicationQualitySystem.Application.DTOs.Role;

public class UpdateRoleDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public ISet<long>? PermissionIds { get; set; }
}
