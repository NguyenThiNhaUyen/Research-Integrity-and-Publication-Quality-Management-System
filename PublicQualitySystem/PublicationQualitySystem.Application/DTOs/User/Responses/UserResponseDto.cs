using PublicationQualitySystem.Application.DTOs.Role.Responses;

namespace PublicationQualitySystem.Application.DTOs.User.Responses;

public class UserResponseDto
{
    public string? Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public ISet<RoleResponseDto>? Roles { get; set; }
}
