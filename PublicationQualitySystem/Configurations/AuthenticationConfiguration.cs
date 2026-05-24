using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PublicationQualitySystem.Constants;
using PublicationQualitySystem.Extensions;
using PublicationQualitySystem.Options;
using PublicationQualitySystem.Security;

namespace PublicationQualitySystem.Configurations;

public static class AuthenticationConfiguration
{
    public static IServiceCollection AddAuthenticationConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(options =>
        {
            var cognito = new CognitoOptions
            {
                Region = ConfigurationValueResolver.Resolve(configuration["Aws:Cognito:Region"]),
                UserPoolId = ConfigurationValueResolver.Resolve(configuration["Aws:Cognito:UserPoolId"])
            };
            options.Issuer = cognito.Issuer;
        });

        services.AddScoped<CognitoClaimsTransformer>();
        services.AddScoped<PermissionClaimsTransformer>();
        services.AddScoped<JwtAuthenticationEvents>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var cognito = new CognitoOptions
                {
                    Region = ConfigurationValueResolver.Resolve(configuration["Aws:Cognito:Region"]),
                    UserPoolId = ConfigurationValueResolver.Resolve(configuration["Aws:Cognito:UserPoolId"])
                };
                options.Authority = cognito.Issuer;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = cognito.Issuer,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    NameClaimType = ClaimConstants.Email
                };
                options.EventsType = typeof(JwtAuthenticationEvents);
            });

        return services;
    }
}
