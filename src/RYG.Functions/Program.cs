using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using RYG.Application.Extensions;
using RYG.Infrastructure.Extensions;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;

// Enable Serilog self-logging to console with more detail
SelfLog.Enable(msg => Console.WriteLine($"SERILOG INTERNAL: {msg}"));

// Configure Serilog before building
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "RYG.Functions")
    .WriteTo.Console()
    .WriteTo.Seq(
        "http://seq:80",
        apiKey: null,
        restrictedToMinimumLevel: LogEventLevel.Information)
    .CreateLogger();

try
{
    var builder = FunctionsApplication.CreateBuilder(args);

    builder.ConfigureFunctionsWebApplication();

    // Configure Serilog from appsettings.json
    builder.Logging.ClearProviders();
    var logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .CreateLogger();

    builder.Logging.AddSerilog(logger, true);

    // Test logging
    logger.Information("RYG Functions starting - Seq configured via appsettings.json");

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
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}