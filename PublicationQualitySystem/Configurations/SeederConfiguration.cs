using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Extensions;
using PublicationQualitySystem.Options;

namespace PublicationQualitySystem.Configurations;

public static class SeederConfiguration
{
    public static IServiceCollection AddSeederConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AdminOptions>(options =>
        {
            options.FullName = ConfigurationValueResolver.Resolve(configuration["App:Admin:FullName"]);
            options.Email = ConfigurationValueResolver.Resolve(configuration["App:Admin:Email"]);
            options.Password = ConfigurationValueResolver.Resolve(configuration["App:Admin:Password"]);
        });
        services.AddScoped<DataSeeder>();
        return services;
    }

    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DataSeeder");
        var connectionString = ConfigurationValueResolver.Resolve(configuration.GetConnectionString("DefaultConnection"));
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            SafeLogWarning(logger, "Database seeding skipped because DefaultConnection is missing");
            return;
        }

        try
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<DataSeeder>().SeedAsync();
        }
        catch (Exception exception)
        {
            SafeLogWarning(logger, exception, "Database seeding skipped or failed");
        }
    }

    private static void SafeLogWarning(ILogger logger, string message)
    {
        try { logger.LogWarning(message); }
        catch { }
    }

    private static void SafeLogWarning(ILogger logger, Exception exception, string message)
    {
        try { logger.LogWarning(exception, message); }
        catch { }
    }
}
