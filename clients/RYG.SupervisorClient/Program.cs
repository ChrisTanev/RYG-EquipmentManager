using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using RYG.Shared.Events;

// SignalR Hub URL (not Functions API URL)
var signalRHubUrl = args.Length > 0 ? args[0] : "http://localhost:5000/equipmentHub";

Console.WriteLine("=== RYG Supervisor Dashboard ===");
Console.WriteLine($"Connecting to SignalR Hub: {signalRHubUrl}");
Console.WriteLine();

var connection = new HubConnectionBuilder()
    .WithUrl(signalRHubUrl)
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

// Stretch goal: supervisors see equipment with their current orders in real-time
connection.On<JsonElement>("equipmentWithOrders", eventData =>
{
    try
    {
        var dashboardEvent = eventData.Deserialize<IEnumerable<EquipmentWithOrdersEvent>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new JsonException();

        Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] === SUPERVISOR DASHBOARD UPDATE ===");

        foreach (var equipment in dashboardEvent)
        {
            Console.WriteLine($"\nEquipment: {equipment.EquipmentName} (ID: {equipment.EquipmentId})");
            Console.WriteLine($"  State: {equipment.State}");
            Console.WriteLine($"  Current Order: {equipment.CurrentOrderId?.ToString() ?? "None"}");
        }

        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Failed to process dashboard event: {ex.Message}");
    }
});

return 0;