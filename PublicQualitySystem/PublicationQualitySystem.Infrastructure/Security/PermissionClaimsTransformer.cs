using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Infrastructure.Configurations;
using PublicationQualitySystem.Shared.Constants;

namespace PublicationQualitySystem.Infrastructure.Security;

public class PermissionClaimsTransformer(ApplicationDbContext db)
{
    public async Task TransformAsync(ClaimsIdentity identity, string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return;

        var user = await db.Users
            .Include(u => u.Roles)
            .ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(u => u.Email == email);
        if (user is null) return;

        identity.AddClaim(new Claim(ClaimConstants.FullName, user.FullName));
        foreach (var permission in user.Roles.SelectMany(r => r.Permissions).Select(p => p.Name).Distinct())
        {
            identity.AddClaim(new Claim(ClaimConstants.Permission, permission));
        }
    }
}
