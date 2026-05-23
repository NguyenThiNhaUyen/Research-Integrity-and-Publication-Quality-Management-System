using PublicationQualitySystem.Common;
using PublicationQualitySystem.DTOs.Auth;
using PublicationQualitySystem.DTOs.File;
using PublicationQualitySystem.DTOs.ResearchGroup;
using PublicationQualitySystem.DTOs.ResearchProfile;
using PublicationQualitySystem.DTOs.Role;
using PublicationQualitySystem.DTOs.User;

namespace PublicationQualitySystem.Services.Interfaces;

public interface IRoleService : IBaseCrudService<RoleDto>
{
    Task<List<RoleDto>> GetAllRolesAsync(int page, int size);
    Task<List<UserRoleDto>> GetUserRolesAsync(string userId);
    Task<List<UserRoleDto>> AssignRoleToUserAsync(string userId, long roleId);
    Task<List<UserRoleDto>> RemoveRoleFromUserAsync(string userId, long roleId);
    Task<List<UserRoleDto>> ReplaceUserRolesAsync(string userId, UpdateUserRolesDto dto);
}
