using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using PublicationQualitySystem.Constants;
using PublicationQualitySystem.Extensions;

namespace PublicationQualitySystem.Security;

public class JwtAuthenticationEvents(
    CognitoClaimsTransformer cognitoClaimsTransformer,
    PermissionClaimsTransformer permissionClaimsTransformer,
    IConfiguration configuration) : JwtBearerEvents
{
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        if (context.Principal?.Identity is not ClaimsIdentity identity) return;

        var tokenUse = identity.FindFirst("token_use")?.Value;
        var clientId = identity.FindFirst("client_id")?.Value;
        var configuredClientId = ConfigurationValueResolver.Resolve(configuration["Aws:Cognito:ClientId"]);

        if (tokenUse != "access" || clientId != configuredClientId)
        {
            context.Fail("Invalid Cognito access token");
            return;
        }

        cognitoClaimsTransformer.Transform(identity, context.Principal);
        var email = identity.FindFirst(ClaimConstants.Email)?.Value;
        await permissionClaimsTransformer.TransformAsync(identity, email);
    }
}
