namespace PublicationQualitySystem.Extensions;

public static class HttpContextExtensions
{
    public static string? GetBearerToken(this HttpContext context)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        return authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization["Bearer ".Length..].Trim()
            : null;
    }
}
