using PublicationQualitySystem.Infrastructure.Configurations;

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
    public async Task<UserResponse> CreateAsync(CreateUserRequest request)
    {
        if (await users.ExistsByEmailAsync(request.Email))
            throw new AppException(UserErrorCode.EmailAlreadyExists);
        if (string.IsNullOrWhiteSpace(request.Id))
            throw new AppException(UserErrorCode.ValidationError, "User id must be Cognito sub");

        var user = new User
        {
            Id = request.Id,
            FullName = request.FullName,
            Email = request.Email,
            Password = string.IsNullOrWhiteSpace(request.Password) ? string.Empty : BCrypt.Net.BCrypt.HashPassword(request.Password)
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return UserMapper.ToResponse(user);
    }

    public async Task<UserResponse> GetByIdAsync(string id)
    {
        var user = await users.FindByIdAsync(id) ?? throw new AppException(UserErrorCode.UserNotFound);
        return UserMapper.ToResponse(user);
    }

    public async Task<UserResponse> UpdateAsync(string id, UpdateUserRequest request)
    {
        var user = await users.FindByIdAsync(id) ?? throw new AppException(UserErrorCode.UserNotFound);
        UserMapper.UpdateEntity(user, request);
        await db.SaveChangesAsync();
        return UserMapper.ToResponse(user);
    }

    public async Task DeleteAsync(string id)
    {
        var user = await users.FindByIdAsync(id) ?? throw new AppException(UserErrorCode.UserNotFound);
        user.Deleted = true;
        await db.SaveChangesAsync();
    }

    public async Task<List<UserResponse>> GetAllAsync(int page, int size)
    {
        var list = await users.FindAllAsync(Math.Max(0, page) * Math.Max(1, size), Math.Max(1, size));
        return list.Select(UserMapper.ToResponse).ToList();
    }
}
