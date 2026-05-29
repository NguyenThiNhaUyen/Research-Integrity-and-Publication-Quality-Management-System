using PublicationQualitySystem.Application.DTOs.Role.Responses;
using PublicationQualitySystem.Domain.Entities;

namespace PublicationQualitySystem.Application.Mappings;

public static class RoleMapper
{
    public static RoleResponseDto ToDto(Role role) => new()
    {
        Id = role.Id,
        Name = role.Name,
        Description = role.Description,
        Permissions = role.Permissions.Select(p => p.Name).ToHashSet()
    };

    public static UserRoleResponseDto ToUserRoleDto(Role role) => new()
    {
        Id = role.Id,
        Name = role.Name,
        Description = role.Description,
        Permissions = role.Permissions.Select(p => p.Name).ToHashSet()
    };
}
