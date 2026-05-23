using Microsoft.AspNetCore.Authorization;
using PublicationQualitySystem.Enums;
using PublicationQualitySystem.Security;

namespace PublicationQualitySystem.Configurations;

public static class AuthorizationConfiguration
{
    public static IServiceCollection AddAuthorizationConfiguration(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            foreach (var permission in Enum.GetNames<PermissionName>())
            {
                options.AddPolicy(permission, policy => policy.Requirements.Add(new PermissionRequirement(permission)));
            }
        });

        return services;
    }
}
