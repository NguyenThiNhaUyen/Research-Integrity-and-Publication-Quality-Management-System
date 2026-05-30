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
        services.AddScoped<IResearchProfileService, ResearchProfileService>();
        services.AddScoped<IResearchGroupService, ResearchGroupService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICognitoGroupService, CognitoGroupService>();
        services.AddScoped<ICognitoUserService, CognitoUserService>();
        services.AddScoped<IFileStorageService, S3FileService>();
        services.AddScoped<IUploadService, UploadService>();
        services.AddScoped<IPaperUploadProcessor, PaperUploadProcessor>();
        services.AddScoped<IPaperVersionService, PaperVersionService>();
        services.AddScoped<IPdfOcrService, TextractOcrService>();

        return services;
    }
}
