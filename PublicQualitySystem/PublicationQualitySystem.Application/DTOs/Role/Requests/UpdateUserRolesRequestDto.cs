namespace PublicationQualitySystem.Application.DTOs.Role.Requests;

public class UpdateUserRolesRequestDto
{
    public ISet<long>? RoleIds { get; set; }
}
