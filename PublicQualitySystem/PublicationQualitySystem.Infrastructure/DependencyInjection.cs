using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Infrastructure.Configurations;
using PublicationQualitySystem.Infrastructure.Services.Implementations;

namespace PublicationQualitySystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddDatabaseConfiguration(configuration)
            .AddAwsConfiguration(configuration)
            .AddRepositoryConfiguration()
            .AddInfrastructureServices()
            .AddSeederConfiguration(configuration);

        return services;
    }

    private static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICognitoGroupService, CognitoGroupService>();
        services.AddScoped<ICognitoUserService, CognitoUserService>();

        return services;
    }
}
