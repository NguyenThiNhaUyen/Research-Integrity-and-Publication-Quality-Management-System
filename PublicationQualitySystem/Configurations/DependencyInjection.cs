using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PublicationQualitySystem.Common;
using PublicationQualitySystem.Enums;
using PublicationQualitySystem.Repositories.Implementations;
using PublicationQualitySystem.Repositories.Interfaces;
using PublicationQualitySystem.Services.Implementations;
using PublicationQualitySystem.Services.Interfaces;

namespace PublicationQualitySystem.Configurations;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
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

        services.AddPersistence(configuration);
        services.AddRepositories();
        services.AddDomainServices();
        services.AddAwsClients(configuration);
        services.AddApplicationAuthentication(configuration);
        services.AddApplicationAuthorization();
        services.AddApplicationSwagger();
        services.AddScoped<DataSeeder>();

        return services;
    }

    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DataSeeder");
        var connectionString = ResolveConfigValue(configuration.GetConnectionString("DefaultConnection"));
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

    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = ResolveConfigValue(configuration.GetConnectionString("DefaultConnection"))
                ?? "Host=localhost;Port=5432;Database=publication_quality_system;Username=postgres;Password=postgres";
            options.UseNpgsql(connectionString);
        });
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IResearchGroupMemberRepository, ResearchGroupMemberRepository>();
    }

    private static void AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IResearchProfileService, ResearchProfileService>();
        services.AddScoped<IResearchGroupService, ResearchGroupService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICognitoGroupService, CognitoGroupService>();
        services.AddScoped<ICognitoUserService, CognitoUserService>();
        services.AddScoped<IS3FileService, S3FileService>();
    }

    private static void AddAwsClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IAmazonCognitoIdentityProvider>(_ =>
        {
            var region = RegionEndpoint.GetBySystemName(ResolveConfigValue(configuration["Aws:Cognito:Region"]) ?? "us-east-1");
            var credentials = CreateAwsCredentials(configuration);
            return credentials is null
                ? new AmazonCognitoIdentityProviderClient(region)
                : new AmazonCognitoIdentityProviderClient(credentials, region);
        });

        services.AddSingleton<IAmazonS3>(_ =>
        {
            var region = RegionEndpoint.GetBySystemName(ResolveConfigValue(configuration["Aws:Region"]) ?? "us-east-1");
            var credentials = CreateAwsCredentials(configuration);
            return credentials is null ? new AmazonS3Client(region) : new AmazonS3Client(credentials, region);
        });
    }

    private static void AddApplicationAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var issuer = BuildCognitoIssuer(configuration);
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = issuer;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
                    NameClaimType = "email"
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        if (context.Principal?.Identity is not ClaimsIdentity identity) return;
                        var email = identity.FindFirst("email")?.Value;
                        var groups = context.Principal.FindAll("cognito:groups").Select(c => c.Value);
                        foreach (var group in groups.Where(g => !string.IsNullOrWhiteSpace(g)))
                        {
                            identity.AddClaim(new Claim("role", group.StartsWith("ROLE_") ? group : $"ROLE_{group}"));
                        }

                        var scope = context.Principal.FindFirst("scope")?.Value;
                        if (!string.IsNullOrWhiteSpace(scope))
                        {
                            foreach (var item in scope.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                            {
                                identity.AddClaim(new Claim("scope", $"SCOPE_{item}"));
                            }
                        }

                        if (string.IsNullOrWhiteSpace(email)) return;

                        var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                        var user = await db.Users
                            .Include(u => u.Roles)
                            .ThenInclude(r => r.Permissions)
                            .FirstOrDefaultAsync(u => u.Email == email);
                        if (user is null) return;

                        identity.AddClaim(new Claim("full_name", user.FullName));
                        foreach (var permission in user.Roles.SelectMany(r => r.Permissions).Select(p => p.Name).Distinct())
                        {
                            identity.AddClaim(new Claim("permission", permission));
                        }
                    }
                };
            });
    }

    private static void AddApplicationAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            foreach (var permission in Enum.GetNames<PermissionName>())
            {
                options.AddPolicy(permission, policy => policy.RequireAssertion(context =>
                    context.User.HasClaim("permission", permission) ||
                    context.User.HasClaim("role", "ROLE_ADMIN")));
            }
        });
    }

    private static void AddApplicationSwagger(this IServiceCollection services)
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
    }

    public static string? ResolveConfigValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        if (value.StartsWith("${", StringComparison.Ordinal) && value.EndsWith('}'))
        {
            var key = value[2..^1].Split(':', 2)[0];
            return Environment.GetEnvironmentVariable(key);
        }
        return value;
    }

    private static AWSCredentials? CreateAwsCredentials(IConfiguration configuration)
    {
        var accessKey = ResolveConfigValue(configuration["Aws:AccessKey"]);
        var secretKey = ResolveConfigValue(configuration["Aws:SecretKey"]);
        return string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey)
            ? null
            : new BasicAWSCredentials(accessKey, secretKey);
    }

    private static string? BuildCognitoIssuer(IConfiguration configuration)
    {
        var region = ResolveConfigValue(configuration["Aws:Cognito:Region"]);
        var userPoolId = ResolveConfigValue(configuration["Aws:Cognito:UserPoolId"]);
        return string.IsNullOrWhiteSpace(region) || string.IsNullOrWhiteSpace(userPoolId)
            ? null
            : $"https://cognito-idp.{region}.amazonaws.com/{userPoolId}";
    }
}
