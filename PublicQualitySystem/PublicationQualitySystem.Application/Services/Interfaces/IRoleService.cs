using PublicationQualitySystem.Shared.Common;
using PublicationQualitySystem.Application.DTOs.Auth;
using PublicationQualitySystem.Application.DTOs.File;
using PublicationQualitySystem.Application.DTOs.ResearchGroup;
using PublicationQualitySystem.Application.DTOs.ResearchProfile;
using PublicationQualitySystem.Application.DTOs.Role;
using PublicationQualitySystem.Application.DTOs.User;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IRoleService : IBaseCrudService<CreateRoleRequest, UpdateRoleRequest, RoleResponse, long>
{
    Task<List<RoleResponse>> GetAllRolesAsync(int page, int size);
    Task<List<UserRoleResponse>> GetUserRolesAsync(string userId);
    Task<List<UserRoleResponse>> AssignRoleToUserAsync(string userId, long roleId);
    Task<List<UserRoleResponse>> RemoveRoleFromUserAsync(string userId, long roleId);
    Task<List<UserRoleResponse>> ReplaceUserRolesAsync(string userId, UpdateUserRolesRequest dto);
}
