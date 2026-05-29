namespace PublicationQualitySystem.Application.DTOs.Role.Requests;

public class UpdateRoleRequestDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public ISet<string>? Permissions { get; set; }
}
