using PublicationQualitySystem.DTOs;
using PublicationQualitySystem.Entities;

namespace PublicationQualitySystem.Mappings;

public static class RoleMapper
{
    public static RoleDto ToDto(Role role) => new()
    {
        Id = role.Id,
        Name = role.Name,
        Description = role.Description,
        Permissions = role.Permissions.Select(p => p.Name).ToHashSet()
    };

    public static UserRoleDto ToUserRoleDto(Role role) => new()
    {
        Id = role.Id,
        Name = role.Name,
        Description = role.Description,
        Permissions = role.Permissions.Select(p => p.Name).ToHashSet()
    };
}
