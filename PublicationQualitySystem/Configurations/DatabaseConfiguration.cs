using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Constants;
using PublicationQualitySystem.Extensions;

namespace PublicationQualitySystem.Configurations;

public static class DatabaseConfiguration
{
    public static IServiceCollection AddDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = ConfigurationValueResolver.Resolve(configuration.GetConnectionString("DefaultConnection"))
                ?? AppConstants.DefaultConnectionString;
            options.UseNpgsql(connectionString);
        });

        return services;
    }
}
