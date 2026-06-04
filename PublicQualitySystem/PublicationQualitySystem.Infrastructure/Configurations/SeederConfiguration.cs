using Microsoft.EntityFrameworkCore;
using PublicationQualitySystem.Shared.Extensions;
using PublicationQualitySystem.Infrastructure.Options;

namespace PublicationQualitySystem.Infrastructure.Configurations;

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
            await EnsureDatabaseSchemaAsync(db, configuration, logger);
            await scope.ServiceProvider.GetRequiredService<DataSeeder>().SeedAsync();
        }
        catch (Exception exception)
        {
            SafeLogError(logger, exception, "Database initialization failed");
            throw;
        }
    }

    private static async Task EnsureDatabaseSchemaAsync(
        ApplicationDbContext db,
        IConfiguration configuration,
        ILogger logger)
    {
        var pendingMigrations = (await db.Database.GetPendingMigrationsAsync()).ToArray();
        var autoMigrate = bool.TryParse(configuration["Database:AutoMigrate"], out var configuredAutoMigrate)
            && configuredAutoMigrate;

        if (pendingMigrations.Length > 0)
        {
            SafeLogWarning(
                logger,
                "Database has pending EF migrations: {PendingMigrations}. AutoMigrate={AutoMigrate}",
                string.Join(", ", pendingMigrations),
                autoMigrate);

            if (!autoMigrate)
            {
                throw new InvalidOperationException(
                    $"Database schema is behind the application model. Pending migrations: {string.Join(", ", pendingMigrations)}. " +
                    "Run `dotnet ef database update --project PublicQualitySystem\\PublicationQualitySystem.Infrastructure --startup-project PublicQualitySystem\\PublicationQualitySystem.Api` " +
                    "or set Database:AutoMigrate=true for local development.");
            }

            await db.Database.MigrateAsync();
            SafeLogInformation(logger, "Database migrations applied successfully");
        }

        await ValidateCriticalSchemaAsync(db);
    }

    private static async Task ValidateCriticalSchemaAsync(ApplicationDbContext db)
    {
        var missingObjects = new List<string>();

        foreach (var columnName in new[] { "current_stage", "current_status", "overall_status" })
        {
            if (!await HasColumnAsync(db, "paper_processing_trackers", columnName))
            {
                missingObjects.Add($"paper_processing_trackers.{columnName}");
            }
        }

        if (!await HasTableAsync(db, "paper_processing_events"))
        {
            missingObjects.Add("paper_processing_events");
        }
        else
        {
            foreach (var columnName in new[]
                     {
                         "tracker_id",
                         "paper_id",
                         "paper_version_id",
                         "event_id",
                         "correlation_id",
                         "event_type",
                         "stage",
                         "status",
                         "payload_json"
                     })
            {
                if (!await HasColumnAsync(db, "paper_processing_events", columnName))
                {
                    missingObjects.Add($"paper_processing_events.{columnName}");
                }
            }
        }

        if (missingObjects.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            "Database schema mismatch detected for paper processing workflow tracking. " +
            $"Missing required objects: {string.Join(", ", missingObjects)}. " +
            "Apply migrations RefactorPaperProcessingTrackerToWorkflowEvents and NormalizePaperProcessingEvents before running uploads. " +
            "Command: dotnet ef database update --project PublicQualitySystem\\PublicationQualitySystem.Infrastructure " +
            "--startup-project PublicQualitySystem\\PublicationQualitySystem.Api");
    }

    private static async Task<bool> HasTableAsync(ApplicationDbContext db, string tableName)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State == System.Data.ConnectionState.Closed;
        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                select exists (
                    select 1
                    from information_schema.tables
                    where table_schema = 'public'
                      and table_name = @table_name
                )
                """;
            var parameter = command.CreateParameter();
            parameter.ParameterName = "table_name";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            return command.ExecuteScalar() is true;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task<bool> HasColumnAsync(ApplicationDbContext db, string tableName, string columnName)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State == System.Data.ConnectionState.Closed;
        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                select exists (
                    select 1
                    from information_schema.columns
                    where table_schema = 'public'
                      and table_name = @table_name
                      and column_name = @column_name
                )
                """;
            var tableParameter = command.CreateParameter();
            tableParameter.ParameterName = "table_name";
            tableParameter.Value = tableName;
            command.Parameters.Add(tableParameter);

            var columnParameter = command.CreateParameter();
            columnParameter.ParameterName = "column_name";
            columnParameter.Value = columnName;
            command.Parameters.Add(columnParameter);

            return command.ExecuteScalar() is true;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static void SafeLogWarning(ILogger logger, string message)
    {
        try { logger.LogWarning(message); }
        catch { }
    }

    private static void SafeLogWarning(ILogger logger, string message, params object?[] args)
    {
        try { logger.LogWarning(message, args); }
        catch { }
    }

    private static void SafeLogInformation(ILogger logger, string message)
    {
        try { logger.LogInformation(message); }
        catch { }
    }

    private static void SafeLogError(ILogger logger, Exception exception, string message)
    {
        try { logger.LogError(exception, message); }
        catch { }
    }
}
