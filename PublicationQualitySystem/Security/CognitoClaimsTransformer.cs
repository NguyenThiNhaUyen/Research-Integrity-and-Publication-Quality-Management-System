using System.Security.Claims;
using PublicationQualitySystem.Constants;

namespace PublicationQualitySystem.Security;

public class CognitoClaimsTransformer
{
    public void Transform(ClaimsIdentity identity, ClaimsPrincipal principal)
    {
        var groups = principal.FindAll(ClaimConstants.CognitoGroups).Select(c => c.Value);
        foreach (var group in groups.Where(g => !string.IsNullOrWhiteSpace(g)))
        {
            identity.AddClaim(new Claim(
                ClaimConstants.Role,
                group.StartsWith(ClaimConstants.RolePrefix, StringComparison.Ordinal) ? group : $"{ClaimConstants.RolePrefix}{group}"));
        }

        var scope = principal.FindFirst(ClaimConstants.Scope)?.Value;
        if (string.IsNullOrWhiteSpace(scope)) return;

        foreach (var item in scope.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            identity.AddClaim(new Claim(ClaimConstants.Scope, $"{ClaimConstants.ScopePrefix}{item}"));
        }
    }
}
