using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RYG.Infrastructure.Messaging;
using RYG.Infrastructure.Persistence;
using Serilog;

namespace RYG.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.Seq("http://localhost:5341")
            .CreateLogger();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog();
        });


        var connectionString = configuration.GetConnectionString("Database")
                               ?? "Data Source=equipment.db";

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IEquipmentRepository, EquipmentRepository>();

        // Configure HttpClient for SignalR Hub communication
        var signalRHubUrl = configuration["SignalR:HubUrl"] ?? "http://localhost:5000";
        services.AddHttpClient<ISignalRPublisher, HttpSignalRPublisher>(client =>
        {
            client.BaseAddress = new Uri(signalRHubUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        var serviceBusConnectionString = configuration.GetConnectionString("ServiceBus");
        var topicName = configuration["ServiceBus:TopicName"] ?? "equipment-events";


        services.AddSingleton(sp =>
        {
            var client = new ServiceBusClient(serviceBusConnectionString);
            var logger = sp.GetRequiredService<ILogger<ServiceBusEventPublisher>>();
            return new ServiceBusEventPublisher(client, topicName, logger);
        });
        services.AddSingleton<IEventPublisher>(sp => sp.GetRequiredService<ServiceBusEventPublisher>());

        return services;
    }
}