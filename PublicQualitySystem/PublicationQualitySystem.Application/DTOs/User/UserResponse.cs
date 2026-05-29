using PublicationQualitySystem.Application.DTOs.Role;

namespace PublicationQualitySystem.Application.DTOs.User;

public class UserResponse
{
    public string? Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public ISet<RoleResponse>? Roles { get; set; }
}
