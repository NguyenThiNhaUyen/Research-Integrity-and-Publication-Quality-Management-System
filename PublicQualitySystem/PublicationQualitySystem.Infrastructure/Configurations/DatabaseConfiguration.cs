using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Shared.Constants;
using PublicationQualitySystem.Shared.Extensions;

namespace PublicationQualitySystem.Infrastructure.Configurations;

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
