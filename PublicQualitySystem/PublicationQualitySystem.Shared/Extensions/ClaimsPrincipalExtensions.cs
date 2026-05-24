using System.Security.Claims;
using PublicationQualitySystem.Shared.Constants;

namespace PublicationQualitySystem.Shared.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string? GetEmail(this ClaimsPrincipal principal) =>
        principal.FindFirst(ClaimConstants.Email)?.Value ?? principal.Identity?.Name;

    public static string? GetSubject(this ClaimsPrincipal principal) =>
        principal.FindFirst("sub")?.Value;

    public static bool HasPermission(this ClaimsPrincipal principal, string permission) =>
        principal.HasClaim(ClaimConstants.Permission, permission) ||
        principal.HasClaim(ClaimConstants.Role, PolicyConstants.AdminRole);
}
