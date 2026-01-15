using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using RYG.Shared.Events;

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

// Goal : operators see equipment state changes in real-time
connection.On<JsonElement>("equipmentStateChanged", eventData =>
{
    try
    {
        var stateEvent = eventData.Deserialize<EquipmentStateChangedEvent>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new JsonException();

        Console.WriteLine(
            $"[{DateTime.Now:HH:mm:ss}] EQUIPMENT STATE: {stateEvent.EquipmentName} → {stateEvent.NewState}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Failed to process state event: {ex.Message}");
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
            Console.WriteLine(
                $"[{DateTime.Now:HH:mm:ss}] EQUIPMENT NAME: {eqipment.Name} EQUIPMENT STATE: {eqipment.State}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Failed to process state event: {ex.Message}");
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

        Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] === ORDER PROCESSING ===");
        Console.WriteLine($"Equipment: {processingEvent.EquipmentName}");
        Console.WriteLine($"Order ID: {processingEvent.OrderId}");
        if (processingEvent.ScheduledOrders.Any())
        {
            Console.WriteLine($"\nUpcoming Orders ({processingEvent.ScheduledOrders.Count()}):");
            foreach (var order in processingEvent.ScheduledOrders)
            {
                Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] === ORDER PROCESSING ===");
                Console.WriteLine($"Equipment: {order.EquipmentName}");
                Console.WriteLine($"Order ID: {order.OrderId}");
                Console.WriteLine($"Order ScheduledAt: {order.ScheduledAt}");
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