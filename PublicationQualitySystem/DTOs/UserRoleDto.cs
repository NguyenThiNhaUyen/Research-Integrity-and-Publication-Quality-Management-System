namespace PublicationQualitySystem.DTOs;

public class UserRoleDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ISet<string> Permissions { get; set; } = new HashSet<string>();
}
