# Modular Monolith / Clean Layering Refactor

Trang thai: da tach monolith `PublicationQualitySystem` thanh 5 project trong `PublicQualitySystem/`. Business logic va route API hien tai duoc giu nguyen. Build pass voi `0 Warning(s), 0 Error(s)`.

## A. Cau Truc Solution Moi

```text
PublicQualitySystem/
  PublicationQualitySystem.Api/
    Common/
    Configurations/
    Controllers/
    Extensions/
    Middleware/
    Properties/
    Program.cs
    appsettings.json
    Dockerfile
    PublicationQualitySystem.Api.csproj

  PublicationQualitySystem.Shared/
    Common/
    Constants/
    Exceptions/
    Extensions/
    PublicationQualitySystem.Shared.csproj

  PublicationQualitySystem.Domain/
    Entities/
    Enums/
    PublicationQualitySystem.Domain.csproj

  PublicationQualitySystem.Application/
    DTOs/
    Mappings/
    Repositories/Interfaces/
    Services/Interfaces/
    DependencyInjection.cs
    PublicationQualitySystem.Application.csproj

  PublicationQualitySystem.Infrastructure/
    Configurations/
    Migrations/
    Options/
    Repositories/Implementations/
    Security/
    Services/Implementations/
    DependencyInjection.cs
    GlobalUsings.cs
    PublicationQualitySystem.Infrastructure.csproj
```

## B. File/Folder Da Di Chuyen

| Tu monolith cu | Sang project moi |
|---|---|
| `Program.cs` | `PublicQualitySystem/PublicationQualitySystem.Api/Program.cs` |
| `appsettings.json` | `PublicQualitySystem/PublicationQualitySystem.Api/appsettings.json` |
| `Properties/launchSettings.json` | `PublicQualitySystem/PublicationQualitySystem.Api/Properties/launchSettings.json` |
| `Controllers/*` | `PublicQualitySystem/PublicationQualitySystem.Api/Controllers/*` |
| `Common/ApiBaseController.cs` | `PublicQualitySystem/PublicationQualitySystem.Api/Common/ApiBaseController.cs` |
| `Middleware/ExceptionHandlingMiddleware.cs` | `PublicQualitySystem/PublicationQualitySystem.Api/Middleware/ExceptionHandlingMiddleware.cs` |
| `Extensions/ApplicationBuilderExtensions.cs` | `PublicQualitySystem/PublicationQualitySystem.Api/Extensions/ApplicationBuilderExtensions.cs` |
| `Extensions/HttpContextExtensions.cs` | `PublicQualitySystem/PublicationQualitySystem.Api/Extensions/HttpContextExtensions.cs` |
| `Exceptions/ExceptionExtensions.cs` | `PublicQualitySystem/PublicationQualitySystem.Api/Extensions/ExceptionExtensions.cs` |
| `Configurations/AuthenticationConfiguration.cs` | `PublicQualitySystem/PublicationQualitySystem.Api/Configurations/AuthenticationConfiguration.cs` |
| `Configurations/AuthorizationConfiguration.cs` | `PublicQualitySystem/PublicationQualitySystem.Api/Configurations/AuthorizationConfiguration.cs` |
| `Configurations/SwaggerConfiguration.cs` | `PublicQualitySystem/PublicationQualitySystem.Api/Configurations/SwaggerConfiguration.cs` |
| `Configurations/ServiceConfiguration.cs` | `PublicQualitySystem/PublicationQualitySystem.Api/Configurations/ServiceConfiguration.cs` |
| `Common/BaseEntity.cs` | `PublicQualitySystem/PublicationQualitySystem.Shared/Common/BaseEntity.cs` |
| `Common/BaseResponse.cs` | `PublicQualitySystem/PublicationQualitySystem.Shared/Common/BaseResponse.cs` |
| `Common/IBaseCrudService.cs` | `PublicQualitySystem/PublicationQualitySystem.Shared/Common/IBaseCrudService.cs` |
| `Common/IErrorCode.cs` | `PublicQualitySystem/PublicationQualitySystem.Shared/Common/IErrorCode.cs` |
| `Constants/*` | `PublicQualitySystem/PublicationQualitySystem.Shared/Constants/*` |
| `Exceptions/*ErrorCode.cs`, `AppException.cs` | `PublicQualitySystem/PublicationQualitySystem.Shared/Exceptions/*` |
| `Enums/ErrorCode.cs` | `PublicQualitySystem/PublicationQualitySystem.Shared/Exceptions/ErrorCode.cs` |
| `Extensions/ClaimsPrincipalExtensions.cs` | `PublicQualitySystem/PublicationQualitySystem.Shared/Extensions/ClaimsPrincipalExtensions.cs` |
| `Extensions/ConfigurationValueResolver.cs` | `PublicQualitySystem/PublicationQualitySystem.Shared/Extensions/ConfigurationValueResolver.cs` |
| `Entities/*` | `PublicQualitySystem/PublicationQualitySystem.Domain/Entities/*` |
| `Enums/*` | `PublicQualitySystem/PublicationQualitySystem.Domain/Enums/*` |
| `DTOs/*` | `PublicQualitySystem/PublicationQualitySystem.Application/DTOs/*` |
| `Mappings/*` | `PublicQualitySystem/PublicationQualitySystem.Application/Mappings/*` |
| `Repositories/Interfaces/*` | `PublicQualitySystem/PublicationQualitySystem.Application/Repositories/Interfaces/*` |
| `Services/Interfaces/*` | `PublicQualitySystem/PublicationQualitySystem.Application/Services/Interfaces/*` |
| `Configurations/ApplicationDbContext.cs` | `PublicQualitySystem/PublicationQualitySystem.Infrastructure/Configurations/ApplicationDbContext.cs` |
| `Configurations/AwsConfiguration.cs` | `PublicQualitySystem/PublicationQualitySystem.Infrastructure/Configurations/AwsConfiguration.cs` |
| `Configurations/DatabaseConfiguration.cs` | `PublicQualitySystem/PublicationQualitySystem.Infrastructure/Configurations/DatabaseConfiguration.cs` |
| `Configurations/DataSeeder.cs` | `PublicQualitySystem/PublicationQualitySystem.Infrastructure/Configurations/DataSeeder.cs` |
| `Configurations/RepositoryConfiguration.cs` | `PublicQualitySystem/PublicationQualitySystem.Infrastructure/Configurations/RepositoryConfiguration.cs` |
| `Configurations/SeederConfiguration.cs` | `PublicQualitySystem/PublicationQualitySystem.Infrastructure/Configurations/SeederConfiguration.cs` |
| `Migrations/*` | `PublicQualitySystem/PublicationQualitySystem.Infrastructure/Migrations/*` |
| `Options/*` | `PublicQualitySystem/PublicationQualitySystem.Infrastructure/Options/*` |
| `Repositories/Implementations/*` | `PublicQualitySystem/PublicationQualitySystem.Infrastructure/Repositories/Implementations/*` |
| `Security/*` | `PublicQualitySystem/PublicationQualitySystem.Infrastructure/Security/*` |
| `Services/Implementations/*` | `PublicQualitySystem/PublicationQualitySystem.Infrastructure/Services/Implementations/*` |
| `Dockerfile` | `PublicQualitySystem/PublicationQualitySystem.Api/Dockerfile` |

## C. Project References

| Project | References |
|---|---|
| `PublicationQualitySystem.Api` | `Application`, `Infrastructure`, `Shared` |
| `PublicationQualitySystem.Application` | `Domain`, `Shared` |
| `PublicationQualitySystem.Infrastructure` | `Application`, `Domain`, `Shared` |
| `PublicationQualitySystem.Domain` | `Shared` |
| `PublicationQualitySystem.Shared` | None |

Khong co circular dependency.

## D. DependencyInjection.cs

### Application

File: `PublicQualitySystem/PublicationQualitySystem.Application/DependencyInjection.cs`

```csharp
public static IServiceCollection AddApplication(this IServiceCollection services)
{
    return services;
}
```

### Infrastructure

File: `PublicQualitySystem/PublicationQualitySystem.Infrastructure/DependencyInjection.cs`

```csharp
public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
{
    services
        .AddDatabaseConfiguration(configuration)
        .AddAwsConfiguration(configuration)
        .AddRepositoryConfiguration()
        .AddInfrastructureServices()
        .AddSeederConfiguration(configuration);

    return services;
}
```

Infrastructure hien dang register repository implementations, business service implementations hien co, Cognito/S3 integration, DbContext va seeder.

### Api

File: `PublicQualitySystem/PublicationQualitySystem.Api/Configurations/DependencyInjection.cs`

```csharp
public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
{
    services
        .AddAuthenticationConfiguration(configuration)
        .AddAuthorizationConfiguration()
        .AddSwaggerConfiguration()
        .AddApiServiceConfiguration();

    return services;
}
```

Program startup:

```csharp
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApi(builder.Configuration);
```

## E. EF Migration/Database Commands Moi

```bash
dotnet ef migrations add <Name> --project PublicQualitySystem/PublicationQualitySystem.Infrastructure --startup-project PublicQualitySystem/PublicationQualitySystem.Api
dotnet ef database update --project PublicQualitySystem/PublicationQualitySystem.Infrastructure --startup-project PublicQualitySystem/PublicationQualitySystem.Api
```

DbContext hien nam tai:

```text
PublicQualitySystem/PublicationQualitySystem.Infrastructure/Configurations/ApplicationDbContext.cs
```

Migrations hien nam tai:

```text
PublicQualitySystem/PublicationQualitySystem.Infrastructure/Migrations
```

## F. Dockerfile Multi-Project

Dockerfile da duoc cap nhat tai:

```text
PublicQualitySystem/PublicationQualitySystem.Api/Dockerfile
```

Dockerfile moi:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /PublicQualitySystem
COPY ["PublicationQualitySystem.sln", "./"]
COPY ["PublicQualitySystem/PublicationQualitySystem.Api/PublicationQualitySystem.Api.csproj", "PublicQualitySystem/PublicationQualitySystem.Api/"]
COPY ["PublicQualitySystem/PublicationQualitySystem.Application/PublicationQualitySystem.Application.csproj", "PublicQualitySystem/PublicationQualitySystem.Application/"]
COPY ["PublicQualitySystem/PublicationQualitySystem.Domain/PublicationQualitySystem.Domain.csproj", "PublicQualitySystem/PublicationQualitySystem.Domain/"]
COPY ["PublicQualitySystem/PublicationQualitySystem.Infrastructure/PublicationQualitySystem.Infrastructure.csproj", "PublicQualitySystem/PublicationQualitySystem.Infrastructure/"]
COPY ["PublicQualitySystem/PublicationQualitySystem.Shared/PublicationQualitySystem.Shared.csproj", "PublicQualitySystem/PublicationQualitySystem.Shared/"]
RUN dotnet restore "PublicQualitySystem/PublicationQualitySystem.Api/PublicationQualitySystem.Api.csproj"
COPY . .
RUN dotnet build "PublicQualitySystem/PublicationQualitySystem.Api/PublicationQualitySystem.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "PublicQualitySystem/PublicationQualitySystem.Api/PublicationQualitySystem.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PublicationQualitySystem.Api.dll"]
```

`docker-compose.yml` da duoc cap nhat:

```yaml
build:
  context: .
  dockerfile: PublicQualitySystem/PublicationQualitySystem.Api/Dockerfile
```

## G. Compile Errors Da Gap Va Cach Fix

| Loi | Nguyen nhan | Cach fix |
|---|---|---|
| `NU1301` khi restore/build | Sandbox chan NuGet network | Build lai voi network approval |
| Thieu `IServiceCollection`, `IConfiguration`, `ILogger`, `WebApplication` trong Infrastructure | Class library can explicit usings/framework ref | Them `FrameworkReference Microsoft.AspNetCore.App` va `GlobalUsings.cs` |
| `Enums.AcademicRank` / `Enums.MemberStatus` khong ton tai | Namespace doi sang `PublicationQualitySystem.Domain.Enums` | Import enum namespace va dung `AcademicRank.RESEARCHER`, `MemberStatus.ACTIVE` |
| EF Core relational version conflict warning | EF Core `8.0.18` va transitive relational `8.0.11` | Them explicit `Microsoft.EntityFrameworkCore.Relational` `8.0.18` |

Ket qua cuoi:

```text
dotnet build PublicationQualitySystem.sln
Build succeeded.
0 Warning(s)
0 Error(s)
```

## H. Checklist Test Sau Refactor

| Test | Status | Lenh/Ghi chu |
|---|---|---|
| Restore/build solution | Passed | `dotnet build PublicationQualitySystem.sln` |
| Test projects | Passed/no test projects detected | `dotnet test PublicationQualitySystem.sln --no-build` |
| Run Api | Todo manual | `dotnet run --project PublicQualitySystem/PublicationQualitySystem.Api` |
| Open Swagger | Todo manual | `https://localhost:7210/swagger` hoac port theo launch profile |
| EF migration add | Todo khi can | Xem lenh EF moi o muc E |
| EF database update | Todo khi co DB | Xem lenh EF moi o muc E |
| Docker build | Todo manual | `docker compose up --build` |
| Postman auth login | Todo manual | Route khong doi |
| Postman user/role/research group API | Todo manual | Route khong doi |
| Upload S3 API | Todo manual | Contract HTTP khong doi |

## Notes

| Chu de | Ghi chu |
|---|---|
| API contract | Route/controller action/body response duoc giu nguyen |
| Upload internals | `IFormFile` duoc map sang `FileUploadRequest` de Application khong phu thuoc ASP.NET Core |
| Service implementations | Dang nam trong Infrastructure vi hien phu thuoc `ApplicationDbContext`, AWS SDK va external services |
| Schema | Khong tao migration moi, khong doi database schema |
| Postman/Swagger | Route khong doi, file Postman van dung duoc voi base URL cu |

