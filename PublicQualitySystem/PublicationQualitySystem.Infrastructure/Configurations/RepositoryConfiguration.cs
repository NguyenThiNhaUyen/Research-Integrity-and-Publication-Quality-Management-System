using PublicationQualitySystem.Infrastructure.Repositories.Implementations;
using PublicationQualitySystem.Application.Repositories.Interfaces;

namespace PublicationQualitySystem.Infrastructure.Configurations;

public static class RepositoryConfiguration
{
    public static IServiceCollection AddRepositoryConfiguration(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IResearchGroupMemberRepository, ResearchGroupMemberRepository>();
        services.AddScoped<IPaperRepository, PaperRepository>();
        services.AddScoped<IPaperVersionRepository, PaperVersionRepository>();
        services.AddScoped<IUploadedFileRepository, UploadedFileRepository>();
        return services;
    }
}
