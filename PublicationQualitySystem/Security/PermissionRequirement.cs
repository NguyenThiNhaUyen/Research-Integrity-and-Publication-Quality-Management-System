using Microsoft.AspNetCore.Authorization;

namespace PublicationQualitySystem.Security;

public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
