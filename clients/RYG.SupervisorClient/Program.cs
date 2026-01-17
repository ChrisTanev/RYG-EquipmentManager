using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using RYG.Shared.Events;
using Serilog;

// Configure Serilog
var seqUrl = Environment.GetEnvironmentVariable("SEQ_URL") ?? "http://localhost:5341";
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.WithProperty("Application", "RYG.SupervisorClient")
    .WriteTo.Console()
    .WriteTo.Seq(seqUrl)
    .CreateLogger();

Log.Information("Supervisor Client starting with Seq URL: {SeqUrl}", seqUrl);

// SignalR Hub URL (not Functions API URL)
var signalRHubUrl = args.Length > 0 ? args[0] : "http://localhost:5000/equipmentHub";

Log.Information("=== RYG Supervisor Dashboard ===");
Log.Information($"Connecting to SignalR Hub: {signalRHubUrl}");

var connection = new HubConnectionBuilder()
    .WithUrl(signalRHubUrl)
    .WithAutomaticReconnect()
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddSerilog(Log.Logger);
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .Build();

connection.Closed += error =>
{
    Log.Warning("Connection closed: {ErrorMessage}", error?.Message);
    Log.Information($"[{DateTime.Now:HH:mm:ss}] Connection closed: {error?.Message}");
    return Task.CompletedTask;
};

connection.Reconnecting += _ =>
{
    Log.Information("Reconnecting to SignalR hub...");
    return Task.CompletedTask;
};

connection.Reconnected += connectionId =>
{
    Log.Information("Reconnected to SignalR hub: {ConnectionId}", connectionId);
    return Task.CompletedTask;
};

// Stretch goal: supervisors see equipment with their current orders in real-time
connection.On<JsonElement>("equipmentWithOrders", eventData =>
{
    try
    {
        var dashboardEvent = eventData.Deserialize<IEnumerable<EquipmentWithOrdersEvent>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new JsonException();

        var equipmentWithOrdersEvents = dashboardEvent.ToList();
        Log.Information("Equipment dashboard update received with {Count} items", equipmentWithOrdersEvents.Count());

        foreach (var equipment in equipmentWithOrdersEvents)
        {
            Log.Information($"\nEquipment: {equipment.EquipmentName} (ID: {equipment.EquipmentId})");
            Log.Information($"  State: {equipment.State}");
            Log.Information($"  Current Order: {equipment.CurrentOrderId?.ToString() ?? "None"}");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to process equipment dashboard event");
    }
});

try
{
    await connection.StartAsync();
    Log.Information("Connected to SignalR hub");
    Log.Information($"[{DateTime.Now:HH:mm:ss}] Connected to SignalR hub");
    Log.Information("Listening for supervisor dashboard events...");
    Log.Information("Press Ctrl+C to exit");

    // Keep running
    await Task.Delay(Timeout.Infinite);
}
catch (Exception ex)
{
    Log.Error($"[ERROR] Connection failed: {ex.Message}");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;