using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using PublicationQualitySystem.Common;
using PublicationQualitySystem.Services.Implementations;
using PublicationQualitySystem.Services.Interfaces;

namespace PublicationQualitySystem.Configurations;

public static class ServiceConfiguration
{
    public static IServiceCollection AddServiceConfiguration(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var message = context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault() ?? "Validation error";
                return new BadRequestObjectResult(BaseResponse<object>.Error(StatusCodes.Status400BadRequest, message));
            };
        });

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IResearchProfileService, ResearchProfileService>();
        services.AddScoped<IResearchGroupService, ResearchGroupService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICognitoGroupService, CognitoGroupService>();
        services.AddScoped<ICognitoUserService, CognitoUserService>();
        services.AddScoped<IFileStorageService, S3FileService>();
        services.AddScoped<IUploadService, UploadService>();

        return services;
    }
}
