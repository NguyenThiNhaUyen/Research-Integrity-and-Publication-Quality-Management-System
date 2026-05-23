using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using PublicationQualitySystem.Constants;

namespace PublicationQualitySystem.Security;

public class JwtAuthenticationEvents(
    CognitoClaimsTransformer cognitoClaimsTransformer,
    PermissionClaimsTransformer permissionClaimsTransformer) : JwtBearerEvents
{
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        if (context.Principal?.Identity is not ClaimsIdentity identity) return;

        cognitoClaimsTransformer.Transform(identity, context.Principal);
        var email = identity.FindFirst(ClaimConstants.Email)?.Value;
        await permissionClaimsTransformer.TransformAsync(identity, email);
    }
}
