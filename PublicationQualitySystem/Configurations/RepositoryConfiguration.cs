using PublicationQualitySystem.Repositories.Implementations;
using PublicationQualitySystem.Repositories.Interfaces;

namespace PublicationQualitySystem.Configurations;

public static class RepositoryConfiguration
{
    public static IServiceCollection AddRepositoryConfiguration(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IResearchGroupMemberRepository, ResearchGroupMemberRepository>();
        return services;
    }
}
