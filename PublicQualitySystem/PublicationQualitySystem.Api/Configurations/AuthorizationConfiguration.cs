using Microsoft.AspNetCore.Authorization;
using PublicationQualitySystem.Domain.Enums;
using PublicationQualitySystem.Infrastructure.Security;

namespace PublicationQualitySystem.Api.Configurations;

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
