namespace PublicationQualitySystem.Configurations;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddDatabaseConfiguration(configuration)
            .AddAwsConfiguration(configuration)
            .AddAuthenticationConfiguration(configuration)
            .AddAuthorizationConfiguration()
            .AddSwaggerConfiguration()
            .AddRepositoryConfiguration()
            .AddServiceConfiguration()
            .AddSeederConfiguration(configuration);

        return services;
    }
}
