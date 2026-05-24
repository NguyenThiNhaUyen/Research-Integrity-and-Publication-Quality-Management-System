using Microsoft.OpenApi.Models;

namespace PublicationQualitySystem.Api.Configurations;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Publication Quality System API",
                Version = "1.0.0",
                Description = "API Documentation for Publication Quality Assurance System"
            });
            options.AddSecurityDefinition("Bearer Authentication", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer Authentication" } }] = []
            });
        });

        return services;
    }
}
