namespace PublicationQualitySystem.Application.DTOs.Role;

public class UpdateRoleRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public ISet<string>? Permissions { get; set; }
}
