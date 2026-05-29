using PublicationQualitySystem.Shared.Common;
using PublicationQualitySystem.Application.DTOs.Role.Requests;
using PublicationQualitySystem.Application.DTOs.Role.Responses;

namespace PublicationQualitySystem.Application.Services.Interfaces;

public interface IRoleService : IBaseCrudService<RoleResponseDto, CreateRoleRequestDto, UpdateRoleRequestDto>
{
    Task<List<RoleResponseDto>> GetAllRolesAsync(int page, int size);
    Task<List<UserRoleResponseDto>> GetUserRolesAsync(string userId);
    Task<List<UserRoleResponseDto>> AssignRoleToUserAsync(string userId, long roleId);
    Task<List<UserRoleResponseDto>> RemoveRoleFromUserAsync(string userId, long roleId);
    Task<List<UserRoleResponseDto>> ReplaceUserRolesAsync(string userId, UpdateUserRolesRequestDto dto);
}
