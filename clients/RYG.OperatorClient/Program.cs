using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using RYG.Shared.Events;
using Serilog;

// TODO correlation id for tracing
// Configure Serilog
var seqUrl = Environment.GetEnvironmentVariable("SEQ_URL") ?? "http://localhost:5341";
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.WithProperty("Application", "RYG.OperatorClient")
    .WriteTo.Console()
    .WriteTo.Seq(seqUrl)
    .CreateLogger();

Log.Information("Operator Client starting with Seq URL: {SeqUrl}", seqUrl);

// SignalR Hub URL (not Functions API URL)
var signalRHubUrl = args.Length > 0 ? args[0] : "http://localhost:5000/equipmentHub";

Log.Information("=== RYG Operator Dashboard ===");
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
    Log.Information("[{Now:HH:mm:ss}] Reconnecting...", DateTime.Now);
    return Task.CompletedTask;
};

connection.Reconnected += connectionId =>
{
    Log.Information("Reconnected to SignalR hub: {ConnectionId}", connectionId);
    Log.Information($"[{DateTime.Now:HH:mm:ss}] Reconnected: {connectionId}");
    return Task.CompletedTask;
};

// Goal : operators see equipment state changes in real-time
connection.On<JsonElement>("equipmentStateChanged", eventData =>
{
    try
    {
        var stateEvent = eventData.Deserialize<EquipmentStateChangedEvent>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new JsonException();
        Log.Information("********************OPERATOR - Equipment state changed ********************");
        Log.Information("Equipment state changed: {EquipmentName} → {NewState}",
            stateEvent.EquipmentName, stateEvent.NewState);
        Log.Information("**************************************************************************");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to process equipment state event");
    }
});

// Goal : operators see an overview of all equipment states periodically
connection.On<JsonElement>("equipmentStatesOverview", eventData =>
{
    try
    {
        var equipmentStatesOverviewEvent = eventData.Deserialize<IEnumerable<EquipmentStatesOverviewEvent>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new JsonException();

        foreach (var eqipment in equipmentStatesOverviewEvent)
        {
            Log.Information("********************OPERATOR - Equipment state overview ********************");

            Log.Information(
                $"[{DateTime.Now:HH:mm:ss}] Equipment name → {eqipment.Name} Equipment state → {eqipment.State}");
            Log.Information("****************************************************************************");
        }
    }
    catch (Exception ex)
    {
        Log.Error($"[ERROR] Failed to process state event: {ex.Message}");
    }
});

// Stretch : operators see current order processing events and which orders are scheduled next
connection.On<JsonElement>("orderProcessing", eventData =>
{
    try
    {
        var processingEvent = eventData.Deserialize<OrderProcessingEvent>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (processingEvent is null) return;

        Log.Information("********************OPERATOR - Order processing ********************");

        Log.Information($"\n[{DateTime.Now:HH:mm:ss}] === ORDER PROCESSING ===");
        Log.Information($"Equipment: {processingEvent.EquipmentName}");
        Log.Information($"Order ID: {processingEvent.OrderId}");
        if (processingEvent.ScheduledOrders.Any())
        {
            Log.Information($"\nUpcoming Orders ({processingEvent.ScheduledOrders.Count()}):");
            foreach (var order in processingEvent.ScheduledOrders)
            {
                Log.Information($"Equipment: {order.EquipmentName}");
                Log.Information($"Equipment Id: {order.EquipmentId}");
                Log.Information($"Order ID: {order.OrderId}");
                Log.Information($"Order ScheduledAt: {order.ScheduledAt}");
            }
        }
        else
        {
            Log.Information("\nNo scheduled orders.");
        }

        Log.Information("*********************************************************");
    }
    catch (Exception ex)
    {
        Log.Information($"[ERROR] Failed to process order event: {ex.Message}");
    }
});

try
{
    await connection.StartAsync();
    Log.Information("Connected to SignalR hub");
    Log.Information($"[{DateTime.Now:HH:mm:ss}] Connected to SignalR hub");

    // Join the operators group
    await connection.InvokeAsync("JoinGroup", "operators");
    Log.Information("Joined 'operators' group");

    Log.Information("Listening for order processing events...");
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