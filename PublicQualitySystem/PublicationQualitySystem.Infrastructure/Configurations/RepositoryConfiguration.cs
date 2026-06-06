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
        services.AddScoped<IPaperRepository, PaperRepository>();
        services.AddScoped<IPaperVersionRepository, PaperVersionRepository>();
        services.AddScoped<IProcessingTrackerRepository, ProcessingTrackerRepository>();
        services.AddScoped<IMetadataQualityRepository, MetadataQualityRepository>();
        services.AddScoped<IOpenAlexRepository, OpenAlexRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        return services;
    }
}
