using PublicationQualitySystem.Common;
using PublicationQualitySystem.DTOs;

namespace PublicationQualitySystem.Services;

public interface IRoleService : IBaseCrudService<RoleDto>
{
    Task<List<RoleDto>> GetAllRolesAsync(int page, int size);
    Task<List<UserRoleDto>> GetUserRolesAsync(string userId);
    Task<List<UserRoleDto>> AssignRoleToUserAsync(string userId, long roleId);
    Task<List<UserRoleDto>> RemoveRoleFromUserAsync(string userId, long roleId);
    Task<List<UserRoleDto>> ReplaceUserRolesAsync(string userId, UpdateUserRolesDto dto);
}
