using PublicationQualitySystem.Shared.Common;
using PublicationQualitySystem.Application.DTOs.Auth;
using PublicationQualitySystem.Application.DTOs.File;
using PublicationQualitySystem.Application.DTOs.ResearchGroup;
using PublicationQualitySystem.Application.DTOs.ResearchProfile;
using PublicationQualitySystem.Application.DTOs.Role;
using PublicationQualitySystem.Application.DTOs.User;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IRoleService : IBaseCrudService<RoleDto>
{
    Task<List<RoleDto>> GetAllRolesAsync(int page, int size);
    Task<List<UserRoleDto>> GetUserRolesAsync(string userId);
    Task<List<UserRoleDto>> AssignRoleToUserAsync(string userId, long roleId);
    Task<List<UserRoleDto>> RemoveRoleFromUserAsync(string userId, long roleId);
    Task<List<UserRoleDto>> ReplaceUserRolesAsync(string userId, UpdateUserRolesDto dto);
}
