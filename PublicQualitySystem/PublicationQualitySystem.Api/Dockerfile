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
