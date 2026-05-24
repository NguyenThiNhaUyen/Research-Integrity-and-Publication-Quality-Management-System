using PublicationQualitySystem.Infrastructure.Configurations;
using PublicationQualitySystem.Shared.Exceptions;

namespace PublicationQualitySystem.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplication ConfigureMiddlewarePipeline(this WebApplication app)
    {
        app.UseGlobalExceptionHandling();
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        return app;
    }

    public static Task InitializeDatabaseAsync(this WebApplication app) =>
        app.SeedDatabaseAsync();
}
