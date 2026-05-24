namespace PublicationQualitySystem.Api.Configurations;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddAuthenticationConfiguration(configuration)
            .AddAuthorizationConfiguration()
            .AddSwaggerConfiguration()
            .AddApiServiceConfiguration();

        return services;
    }
}
