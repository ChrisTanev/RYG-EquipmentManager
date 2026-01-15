using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

var baseUrl = args.Length > 0 ? args[0] : "http://localhost:7071/api";

Console.WriteLine("=== RYG Supervisor Dashboard ===");
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

// Listen to supervisor dashboard events
connection.On<JsonElement>("supervisorDashboard", eventData =>
{
    try
    {
        var dashboardEvent = eventData.Deserialize<SupervisorDashboardEvent>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (dashboardEvent is null) return;

        Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] === SUPERVISOR DASHBOARD UPDATE ===");

        foreach (var equipment in dashboardEvent.EquipmentStates)
        {
            var stateColor = equipment.State switch
            {
                0 => "RED",
                1 => "YELLOW",
                2 => "GREEN",
                _ => "UNKNOWN"
            };

            Console.WriteLine($"\nEquipment: {equipment.EquipmentName} (ID: {equipment.EquipmentId})");
            Console.WriteLine($"  State: {stateColor}");
            Console.WriteLine($"  Current Order: {equipment.CurrentOrderId?.ToString() ?? "None"}");

            if (equipment.ScheduledOrders.Any())
            {
                Console.WriteLine($"  Scheduled Orders ({equipment.ScheduledOrders.Count()}):");
                foreach (var order in equipment.ScheduledOrders)
                    Console.WriteLine(
                        $"    - {order.Description} (Priority: {order.Priority}, Scheduled: {order.ScheduledAt:yyyy-MM-dd HH:mm})");
            }
            else
            {
                Console.WriteLine("  Scheduled Orders: None");
            }
        }

        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Failed to process dashboard event: {ex.Message}");
    }
});

// Also listen to order processing events for real-time updates
connection.On<JsonElement>("orderProcessing", eventData =>
{
    try
    {
        var processingEvent = eventData.Deserialize<OrderProcessingEvent>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (processingEvent is null) return;

        Console.WriteLine(
            $"[{DateTime.Now:HH:mm:ss}] ORDER PROCESSING: {processingEvent.EquipmentName} - {processingEvent.OrderDescription}");
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
    Console.WriteLine("Listening for supervisor dashboard events...");
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

// Event DTOs (copy from domain layer)
public record SupervisorDashboardEvent(IEnumerable<EquipmentWithOrdersInfo> EquipmentStates);

public record EquipmentWithOrdersInfo(
    Guid EquipmentId,
    string EquipmentName,
    int State,
    Guid? CurrentOrderId,
    IEnumerable<ScheduledOrderInfo> ScheduledOrders);

public record ScheduledOrderInfo(Guid OrderId, string Description, int Priority, DateTime ScheduledAt);

public record OrderProcessingEvent(
    Guid OrderId,
    Guid EquipmentId,
    string EquipmentName,
    string OrderDescription,
    DateTime StartedAt,
    IEnumerable<ScheduledOrderInfo> ScheduledOrders);