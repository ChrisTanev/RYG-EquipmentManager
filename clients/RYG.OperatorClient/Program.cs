using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

var baseUrl = args.Length > 0 ? args[0] : "http://localhost:7071/api";

Console.WriteLine("=== RYG Operator Dashboard ===");
Console.WriteLine($"Connecting to: {baseUrl}");
Console.WriteLine();

var connection = new HubConnectionBuilder()
    .WithUrl($"{baseUrl}/negotiate")
    .WithAutomaticReconnect()
    .ConfigureLogging(logging =>
    {
        logging.SetMinimumLevel(LogLevel.Information);
        logging.AddConsole();
    })
    .Build();

connection.Closed += error =>
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Connection closed: {error?.Message}");
    return Task.CompletedTask;
};

connection.Reconnecting += error =>
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Reconnecting...");
    return Task.CompletedTask;
};

connection.Reconnected += connectionId =>
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Reconnected: {connectionId}");
    return Task.CompletedTask;
};

// Listen to order processing events
connection.On<JsonElement>("orderProcessing", eventData =>
{
    try
    {
        var processingEvent = eventData.Deserialize<OrderProcessingEvent>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (processingEvent is null) return;

        Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] === ORDER PROCESSING ===");
        Console.WriteLine($"Equipment: {processingEvent.EquipmentName}");
        Console.WriteLine($"Order ID: {processingEvent.OrderId}");
        Console.WriteLine($"Description: {processingEvent.OrderDescription}");
        Console.WriteLine($"Started At: {processingEvent.StartedAt:yyyy-MM-dd HH:mm:ss}");

        if (processingEvent.ScheduledOrders.Any())
        {
            Console.WriteLine($"\nUpcoming Orders ({processingEvent.ScheduledOrders.Count()}):");
            foreach (var order in processingEvent.ScheduledOrders)
            {
                Console.WriteLine($"  - {order.Description}");
                Console.WriteLine($"    Priority: {order.Priority}, Scheduled: {order.ScheduledAt:yyyy-MM-dd HH:mm}");
            }
        }
        else
        {
            Console.WriteLine("\nUpcoming Orders: None");
        }

        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Failed to process order event: {ex.Message}");
    }
});

// Also listen to equipment state changes
connection.On<JsonElement>("equipmentStateChanged", eventData =>
{
    try
    {
        var stateEvent = eventData.Deserialize<EquipmentStateChangedEvent>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (stateEvent is null) return;

        var stateColor = stateEvent.NewState switch
        {
            0 => "RED",
            1 => "YELLOW",
            2 => "GREEN",
            _ => "UNKNOWN"
        };

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] EQUIPMENT STATE: {stateEvent.EquipmentName} → {stateColor}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Failed to process state event: {ex.Message}");
    }
});

try
{
    await connection.StartAsync();
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Connected to SignalR hub");
    Console.WriteLine("Listening for order processing events...");
    Console.WriteLine("Press Ctrl+C to exit");
    Console.WriteLine();

    // Keep running
    await Task.Delay(Timeout.Infinite);
}
catch (Exception ex)
{
    Console.WriteLine($"[ERROR] Connection failed: {ex.Message}");
    return 1;
}

return 0;

// Event DTOs
public record OrderProcessingEvent(
    Guid OrderId,
    Guid EquipmentId,
    string EquipmentName,
    string OrderDescription,
    DateTime StartedAt,
    IEnumerable<ScheduledOrderInfo> ScheduledOrders);

public record ScheduledOrderInfo(Guid OrderId, string Description, int Priority, DateTime ScheduledAt);

public record EquipmentStateChangedEvent(
    Guid EquipmentId,
    string EquipmentName,
    int NewState,
    Guid? CurrentOrderId,
    DateTime ChangedAt);