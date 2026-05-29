using PublicationQualitySystem.Application.DTOs.User.Requests;
using PublicationQualitySystem.Application.DTOs.User.Responses;
using PublicationQualitySystem.Domain.Entities;

namespace PublicationQualitySystem.Application.Mappings;

public static class UserMapper
{
    public static UserResponseDto ToDto(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Roles = user.Roles.Select(RoleMapper.ToDto).ToHashSet()
    };

    public static void UpdateEntity(User user, UpdateUserRequestDto dto)
    {
        user.FullName = dto.FullName;
        user.Email = dto.Email;
    }
}
