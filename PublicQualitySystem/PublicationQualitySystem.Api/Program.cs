using PublicationQualitySystem.Api.Configurations;
using PublicationQualitySystem.Api.Extensions;
using PublicationQualitySystem.Application;
using PublicationQualitySystem.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApi(builder.Configuration);

var app = builder.Build();

await app.InitializeDatabaseAsync();

app.ConfigureMiddlewarePipeline();

app.Run();
