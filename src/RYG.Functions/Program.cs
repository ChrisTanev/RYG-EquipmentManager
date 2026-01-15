using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using RYG.Application.Extensions;
using RYG.Infrastructure.Extensions;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddSingleton<IOpenApiConfigurationOptions>(_ =>
    new OpenApiConfigurationOptions
    {
        Info = new OpenApiInfo
        {
            Title = "RYG Equipment Manager API",
            Version = "1.0.0",
            Description = "API for managing equipment states (Red/Yellow/Green)"
        }
    });

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

app.Run();