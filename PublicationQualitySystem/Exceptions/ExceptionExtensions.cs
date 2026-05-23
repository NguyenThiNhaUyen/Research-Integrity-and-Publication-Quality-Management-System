using PublicationQualitySystem.Middleware;

namespace PublicationQualitySystem.Exceptions;

public static class ExceptionExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionHandlingMiddleware>();
}
