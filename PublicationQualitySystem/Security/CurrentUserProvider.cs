using System.Security.Claims;
using PublicationQualitySystem.Extensions;

namespace PublicationQualitySystem.Security;

public interface ICurrentUserProvider
{
    string? Email { get; }
    string? Subject { get; }
    ClaimsPrincipal User { get; }
}

public class CurrentUserProvider(IHttpContextAccessor httpContextAccessor) : ICurrentUserProvider
{
    public ClaimsPrincipal User => httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();
    public string? Email => User.GetEmail();
    public string? Subject => User.GetSubject();
}
