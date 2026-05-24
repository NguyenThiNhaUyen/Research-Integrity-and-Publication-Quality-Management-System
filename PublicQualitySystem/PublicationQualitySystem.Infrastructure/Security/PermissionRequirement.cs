using Microsoft.AspNetCore.Authorization;

namespace PublicationQualitySystem.Infrastructure.Security;

public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
