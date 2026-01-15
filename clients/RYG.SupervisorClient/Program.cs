using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using RYG.Shared.Events;

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