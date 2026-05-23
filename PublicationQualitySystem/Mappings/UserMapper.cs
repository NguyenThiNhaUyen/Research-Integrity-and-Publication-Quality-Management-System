using PublicationQualitySystem.DTOs.Auth;
using PublicationQualitySystem.DTOs.File;
using PublicationQualitySystem.DTOs.ResearchGroup;
using PublicationQualitySystem.DTOs.ResearchProfile;
using PublicationQualitySystem.DTOs.Role;
using PublicationQualitySystem.DTOs.User;
using PublicationQualitySystem.Entities;

namespace PublicationQualitySystem.Mappings;

public static class UserMapper
{
    public static UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Roles = user.Roles.Select(RoleMapper.ToDto).ToHashSet()
    };

    public static void UpdateEntity(User user, UserDto dto)
    {
        user.FullName = dto.FullName;
        user.Email = dto.Email;
    }
}
