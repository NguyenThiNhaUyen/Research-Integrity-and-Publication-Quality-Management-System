using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PublicationQualitySystem.Application.Services.Interfaces;
using PublicationQualitySystem.Infrastructure.Configurations;
using PublicationQualitySystem.Infrastructure.Options;
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
        services.AddScoped<IFileStorageService, S3FileStorageService>();
        services.AddScoped<IPaperService, PaperService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IPaperProcessingTrackerService, PaperProcessingTrackerService>();
        services.AddScoped<IMetadataNormalizerService, MetadataNormalizerService>();
        services.AddScoped<IMetadataQualityScoringService, MetadataQualityScoringService>();
        services.AddScoped<IPaperDoiCheckService, PaperDoiCheckService>();
        services.AddScoped<IReferenceNormalizer, ReferenceNormalizer>();
        services.AddScoped<IReferenceQualityService, ReferenceQualityService>();
        services.AddOptions<KafkaOptions>().BindConfiguration("Kafka");
        services.AddOptions<OpenAlexOptions>().BindConfiguration("OpenAlex");
        services.AddHostedService<KafkaTopicInitializerHostedService>();
        services.AddHostedService<KafkaOutboxPublisherBackgroundService>();
        services.AddHostedService<PaperOcrKafkaConsumerBackgroundService>();
        services.AddHostedService<PaperMetadataKafkaConsumerBackgroundService>();
        services.AddHostedService<OpenAlexGateKafkaConsumerBackgroundService>();
        services.AddHostedService<OpenAlexSimilarityKafkaConsumerBackgroundService>();
        services.AddHttpClient<INougatService, NougatService>((provider, client) =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var baseUrl = configuration["Nougat:BaseUrl"] ?? "http://nougat-service:8001";
            var timeoutMinutes = configuration.GetValue("Nougat:TimeoutMinutes", 30);
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromMinutes(timeoutMinutes);
        });
        services.AddHttpClient<IGrobidService, GrobidService>((provider, client) =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var baseUrl = configuration["Grobid:BaseUrl"] ?? "http://grobid:8070";
            var timeoutMinutes = configuration.GetValue("Grobid:TimeoutMinutes", 10);
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromMinutes(timeoutMinutes);
        });
        services.AddHttpClient<ICrossrefService, CrossrefService>((provider, client) =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var baseUrl = configuration["Crossref:BaseUrl"] ?? "https://api.crossref.org";
            var timeoutSeconds = configuration.GetValue("Crossref:TimeoutSeconds", 15);
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("RIPQMS/1.0 (mailto:admin@example.com)");
        });
        services.AddHttpClient<IOpenAlexService, OpenAlexService>((provider, client) =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var baseUrl = configuration["OpenAlex:BaseUrl"] ?? "https://api.openalex.org";
            var timeoutSeconds = configuration.GetValue("OpenAlex:TimeoutSeconds", 20);
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("RIPQMS/1.0 (mailto:admin@example.com)");
        });

        return services;
    }
}
