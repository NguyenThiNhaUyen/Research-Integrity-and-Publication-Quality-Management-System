using PublicationQualitySystem.Application.DTOs.Auth;

using PublicationQualitySystem.Application.DTOs.Role;
using PublicationQualitySystem.Application.DTOs.User;
using PublicationQualitySystem.Domain.Entities;

namespace PublicationQualitySystem.Application.Mappings;

public static class UserMapper
{
    public static UserResponse ToResponse(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Roles = user.Roles.Select(RoleMapper.ToResponse).ToHashSet()
    };

    public static void UpdateEntity(User user, UpdateUserRequest request)
    {
        user.FullName = request.FullName;
        user.Email = request.Email;
    }
}
