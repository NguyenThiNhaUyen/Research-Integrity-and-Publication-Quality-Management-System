using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Infrastructure.Configurations;
using PublicationQualitySystem.Application.DTOs.Role.Requests;
using PublicationQualitySystem.Application.DTOs.Role.Responses;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Shared.Exceptions;
using PublicationQualitySystem.Application.Mappings;
using PublicationQualitySystem.Application.Repositories.Interfaces;
using PublicationQualitySystem.Application.Services.Interfaces;

namespace PublicationQualitySystem.Infrastructure.Services.Implementations;

public class RoleService(
    ApplicationDbContext db,
    IRoleRepository roles,
    IUserRepository users,
    IPermissionRepository permissions,
    ICognitoGroupService cognitoGroups) : IRoleService
{
    private const string AdminRole = "ADMIN";

    public Task<List<RoleResponseDto>> GetAllRolesAsync(int page, int size) => GetAllAsync(page, size);

    public async Task<List<UserRoleResponseDto>> GetUserRolesAsync(string userId)
    {
        var user = await users.FindByIdAsync(userId) ?? throw new AppException(UserErrorCode.UserNotFound);
        return user.Roles.Select(RoleMapper.ToUserRoleDto).ToList();
    }

    public async Task<List<UserRoleResponseDto>> AssignRoleToUserAsync(string userId, long roleId)
    {
        var user = await users.FindByIdAsync(userId) ?? throw new AppException(UserErrorCode.UserNotFound);
        var role = await roles.FindByIdAsync(roleId) ?? throw new AppException(RoleErrorCode.RoleNotFound);
        await cognitoGroups.EnsureGroupExistsAsync(role.Name);
        user.Roles.Add(role);
        await db.SaveChangesAsync();
        await cognitoGroups.AddUserToGroupAsync(user.Email, role.Name);
        return await GetUserRolesAsync(userId);
    }

    public async Task<List<UserRoleResponseDto>> RemoveRoleFromUserAsync(string userId, long roleId)
    {
        var user = await users.FindByIdAsync(userId) ?? throw new AppException(UserErrorCode.UserNotFound);
        var role = await roles.FindByIdAsync(roleId) ?? throw new AppException(RoleErrorCode.RoleNotFound);
        user.Roles.Remove(role);
        await db.SaveChangesAsync();
        await cognitoGroups.RemoveUserFromGroupAsync(user.Email, role.Name);
        return await GetUserRolesAsync(userId);
    }

    public async Task<List<UserRoleResponseDto>> ReplaceUserRolesAsync(string userId, UpdateUserRolesRequestDto dto)
    {
        var user = await users.FindByIdAsync(userId) ?? throw new AppException(UserErrorCode.UserNotFound);
        var roleIds = dto.RoleIds ?? new HashSet<long>();
        var targetRoles = await roles.FindAllByIdsAsync(roleIds);
        if (targetRoles.Count != roleIds.Count) throw new AppException(RoleErrorCode.RoleNotFound);

        var currentRoles = user.Roles.ToHashSet();
        foreach (var role in targetRoles.Where(r => !currentRoles.Any(c => c.Id == r.Id)))
        {
            await cognitoGroups.EnsureGroupExistsAsync(role.Name);
            await cognitoGroups.AddUserToGroupAsync(user.Email, role.Name);
        }
        foreach (var role in currentRoles.Where(r => targetRoles.All(t => t.Id != r.Id)))
        {
            await cognitoGroups.RemoveUserFromGroupAsync(user.Email, role.Name);
        }

        user.Roles = targetRoles.ToHashSet();
        await db.SaveChangesAsync();
        return await GetUserRolesAsync(userId);
    }

    public async Task<RoleResponseDto> CreateAsync(CreateRoleRequestDto dto)
    {
        var roleName = NormalizeRoleName(dto.Name);
        if (await roles.ExistsByNameAsync(roleName)) throw new AppException(RoleErrorCode.RoleAlreadyExists);

        var loadedPermissions = await LoadPermissionsAsync(dto.Permissions);
        await cognitoGroups.CreateGroupAsync(roleName, dto.Description);
        try
        {
            var role = new Role { Name = roleName, Description = dto.Description, Permissions = loadedPermissions };
            db.Roles.Add(role);
            await db.SaveChangesAsync();
            return RoleMapper.ToDto(role);
        }
        catch
        {
            await cognitoGroups.DeleteGroupAsync(roleName);
            throw;
        }
    }

    public async Task<RoleResponseDto> GetByIdAsync(long id)
    {
        var role = await roles.FindByIdAsync(id) ?? throw new AppException(RoleErrorCode.RoleNotFound);
        return RoleMapper.ToDto(role);
    }

    public async Task<RoleResponseDto> UpdateAsync(long id, UpdateRoleRequestDto dto)
    {
        var role = await roles.FindByIdAsync(id) ?? throw new AppException(RoleErrorCode.RoleNotFound);
        var oldName = role.Name;
        var newName = string.IsNullOrWhiteSpace(dto.Name) ? oldName : NormalizeRoleName(dto.Name);
        if (oldName != newName && await roles.ExistsByNameAsync(newName)) throw new AppException(RoleErrorCode.RoleAlreadyExists);

        var usersWithRole = await users.FindByRolesIdAsync(id);
        if (oldName != newName)
        {
            await cognitoGroups.CreateGroupAsync(newName, dto.Description);
            foreach (var user in usersWithRole)
            {
                await cognitoGroups.AddUserToGroupAsync(user.Email, newName);
                await cognitoGroups.RemoveUserFromGroupAsync(user.Email, oldName);
            }
            await cognitoGroups.DeleteGroupAsync(oldName);
        }

        role.Name = newName;
        role.Description = dto.Description;
        if (dto.Permissions is not null) role.Permissions = await LoadPermissionsAsync(dto.Permissions);
        await db.SaveChangesAsync();
        return RoleMapper.ToDto(role);
    }

    public async Task DeleteAsync(long id)
    {
        var role = await roles.FindByIdAsync(id) ?? throw new AppException(RoleErrorCode.RoleNotFound);
        if (role.Name == AdminRole) throw new AppException(RoleErrorCode.RoleInUse, "ADMIN role cannot be deleted");
        var usersWithRole = await users.FindByRolesIdAsync(id);
        foreach (var user in usersWithRole)
        {
            user.Roles.Remove(role);
            await cognitoGroups.RemoveUserFromGroupAsync(user.Email, role.Name);
        }
        role.Deleted = true;
        await cognitoGroups.DeleteGroupAsync(role.Name);
        await db.SaveChangesAsync();
    }

    public async Task<List<RoleResponseDto>> GetAllAsync(int page, int size)
    {
        var list = await roles.FindAllAsync(Math.Max(0, page) * Math.Max(1, size), Math.Max(1, size));
        return list.Select(RoleMapper.ToDto).ToList();
    }

    private static string NormalizeRoleName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new AppException(RoleErrorCode.RoleNameRequired);
        return string.Join("_", value.Trim().ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private async Task<ICollection<Permission>> LoadPermissionsAsync(ISet<string>? permissionNames)
    {
        if (permissionNames is null || permissionNames.Count == 0) return new HashSet<Permission>();
        var loaded = await permissions.FindAllByNamesAsync(permissionNames);
        var found = loaded.Select(p => p.Name).ToHashSet();
        var missing = permissionNames.FirstOrDefault(p => !found.Contains(p));
        if (missing is not null) throw new AppException(RoleErrorCode.ValidationError, $"Permission not found: {missing}");
        return loaded.ToHashSet();
    }
}
