using PublicationQualitySystem.Infrastructure.Configurations;
using PublicationQualitySystem.Application.DTOs.Auth;
using PublicationQualitySystem.Application.DTOs.File;
using PublicationQualitySystem.Application.DTOs.ResearchGroup;
using PublicationQualitySystem.Application.DTOs.ResearchProfile;
using PublicationQualitySystem.Application.DTOs.Role;
using PublicationQualitySystem.Application.DTOs.User;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Shared.Exceptions;
using PublicationQualitySystem.Application.Mappings;
using PublicationQualitySystem.Application.Repositories.Interfaces;
using PublicationQualitySystem.Application.Services.Interfaces;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public class UserService(ApplicationDbContext db, IUserRepository users) : IUserService
{
    public async Task<UserDto> CreateAsync(UserDto dto)
    {
        if (await users.ExistsByEmailAsync(dto.Email))
            throw new AppException(UserErrorCode.EmailAlreadyExists);
        if (string.IsNullOrWhiteSpace(dto.Id))
            throw new AppException(UserErrorCode.ValidationError, "User id must be Cognito sub");

        var user = new User
        {
            Id = dto.Id,
            FullName = dto.FullName,
            Email = dto.Email,
            Password = string.IsNullOrWhiteSpace(dto.Password) ? string.Empty : BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return UserMapper.ToDto(user);
    }

    public async Task<UserDto> GetByIdAsync(string id)
    {
        var user = await users.FindByIdAsync(id) ?? throw new AppException(UserErrorCode.UserNotFound);
        return UserMapper.ToDto(user);
    }

    public async Task<UserDto> UpdateAsync(string id, UserDto dto)
    {
        var user = await users.FindByIdAsync(id) ?? throw new AppException(UserErrorCode.UserNotFound);
        UserMapper.UpdateEntity(user, dto);
        await db.SaveChangesAsync();
        return UserMapper.ToDto(user);
    }

    public async Task DeleteAsync(string id)
    {
        var user = await users.FindByIdAsync(id) ?? throw new AppException(UserErrorCode.UserNotFound);
        user.Deleted = true;
        await db.SaveChangesAsync();
    }

    public async Task<List<UserDto>> GetAllAsync(int page, int size)
    {
        var list = await users.FindAllAsync(Math.Max(0, page) * Math.Max(1, size), Math.Max(1, size));
        return list.Select(UserMapper.ToDto).ToList();
    }
}
