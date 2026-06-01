namespace PublicationQualitySystem.Application.DTOs.Role;

public class UpdateUserRolesRequest
{
    public ISet<long>? RoleIds { get; set; }
}
