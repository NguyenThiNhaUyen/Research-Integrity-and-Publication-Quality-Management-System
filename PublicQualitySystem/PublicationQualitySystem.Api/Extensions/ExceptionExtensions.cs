using PublicationQualitySystem.Api.Middleware;

namespace PublicationQualitySystem.Api.Extensions;

public static class ExceptionExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionHandlingMiddleware>();
}
