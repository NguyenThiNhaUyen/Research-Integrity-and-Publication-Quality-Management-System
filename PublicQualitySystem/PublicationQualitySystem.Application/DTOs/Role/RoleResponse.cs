namespace PublicationQualitySystem.Application.DTOs.Role;

public class RoleResponse
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ISet<string>? Permissions { get; set; }
}
