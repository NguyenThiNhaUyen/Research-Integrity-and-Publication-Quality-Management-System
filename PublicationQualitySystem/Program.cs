using PublicationQualitySystem.Configurations;
using PublicationQualitySystem.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

await app.InitializeDatabaseAsync();

app.ConfigureMiddlewarePipeline();

app.Run();
