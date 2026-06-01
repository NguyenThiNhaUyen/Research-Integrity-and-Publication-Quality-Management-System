using System.Text.Json;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using PublicationQualitySystem.Shared.Common;
using PublicationQualitySystem.Shared.Exceptions;

namespace PublicationQualitySystem.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException exception)
        {
            await WriteError(context, exception.ErrorCode.StatusCode, exception.ErrorCode.Code, exception.Message);
        }
        catch (AmazonCognitoIdentityProviderException exception)
        {
            logger.LogError(exception, "Cognito error");
            var message = $"Cognito error [{exception.ErrorCode ?? "UNKNOWN"}]: {exception.Message}";
            await WriteError(context, AuthErrorCode.CognitoError.StatusCode, AuthErrorCode.CognitoError.Code, message);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled error");
            await WriteError(context, 500, 500, exception.Message);
        }
    }

    private static async Task WriteError(HttpContext context, int statusCode, int code, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var payload = BaseResponse<object>.Error(code, message);
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
