using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Spectre.Console;

var baseUrl = args.Length > 0 ? args[0] : "http://localhost:7071/api";

AnsiConsole.MarkupLine("[bold blue]RYG Equipment Monitor[/]");
AnsiConsole.MarkupLine($"Connecting to: [yellow]{baseUrl}[/]");

var equipmentStates = new Dictionary<Guid, EquipmentStateInfo>();
var eventLog = new List<string>();
const int maxLogEntries = 10;

var connection = new HubConnectionBuilder()
    .WithUrl($"{baseUrl}/negotiate")
    .WithAutomaticReconnect()
    .Build();

connection.On<JsonElement>("equipmentStateChanged", eventData =>
{
    try
    {
        var stateEvent = eventData.Deserialize<EquipmentStateChangedEvent>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (stateEvent is not null)
        {
            equipmentStates[stateEvent.EquipmentId] = new EquipmentStateInfo(
                stateEvent.EquipmentId,
                stateEvent.EquipmentName,
                stateEvent.NewState,
                stateEvent.CurrentOrderId,
                stateEvent.ChangedAt);

            var stateColor = GetStateColor(stateEvent.NewState);
            var logEntry =
                $"[grey]{DateTime.Now:HH:mm:ss}[/] [{stateColor}]{stateEvent.EquipmentName}[/] -> [{stateColor}]{stateEvent.NewState}[/]";

            eventLog.Insert(0, logEntry);
            if (eventLog.Count > maxLogEntries)
                eventLog.RemoveAt(eventLog.Count - 1);
        }
    }
    catch (Exception ex)
    {
        eventLog.Insert(0, $"[red]Error: {ex.Message}[/]");
    }
});

connection.Reconnecting += _ =>
{
    AnsiConsole.MarkupLine("[yellow]Reconnecting...[/]");
    return Task.CompletedTask;
};

connection.Reconnected += _ =>
{
    AnsiConsole.MarkupLine("[green]Reconnected![/]");
    return Task.CompletedTask;
};

connection.Closed += _ =>
{
    AnsiConsole.MarkupLine("[red]Connection closed.[/]");
    return Task.CompletedTask;
};

try
{
    await connection.StartAsync();
    AnsiConsole.MarkupLine("[green]Connected![/]");
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]Failed to connect: {ex.Message}[/]");
    return;
}

AnsiConsole.MarkupLine("[grey]Press Ctrl+C to exit[/]");
AnsiConsole.WriteLine();

await AnsiConsole.Live(new Panel("Waiting for events..."))
    .StartAsync(async ctx =>
    {
        while (connection.State == HubConnectionState.Connected)
        {
            var layout = new Layout("Root")
                .SplitRows(
                    new Layout("Equipment"),
                    new Layout("Log"));

            // Equipment table
            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Equipment")
                .AddColumn("State")
                .AddColumn("Current Order")
                .AddColumn("Last Changed");

            if (equipmentStates.Count == 0)
                table.AddRow("[grey]Waiting for equipment data...[/]", "", "", "");
            else
                foreach (var equipment in equipmentStates.Values.OrderBy(e => e.Name))
                {
                    var stateColor = GetStateColor(equipment.State);
                    var stateDisplay = equipment.State switch
                    {
                        0 => "[white on red]  RED   [/]",
                        1 => "[black on yellow] YELLOW [/]",
                        2 => "[white on green]  GREEN [/]",
                        _ => "[grey]UNKNOWN[/]"
                    };

                    var orderDisplay = equipment.CurrentOrderId.HasValue
                        ? $"[cyan]{equipment.CurrentOrderId.Value.ToString()[..8]}...[/]"
                        : "[grey]None[/]";

                    table.AddRow(
                        $"[bold]{equipment.Name}[/]",
                        stateDisplay,
                        orderDisplay,
                        $"[grey]{equipment.ChangedAt:HH:mm:ss}[/]");
                }

            layout["Equipment"].Update(
                new Panel(table)
                    .Header("[bold blue] Equipment Status [/]")
                    .Border(BoxBorder.Double));

            // Event log
            var logPanel = new Panel(
                    eventLog.Count > 0
                        ? string.Join("\n", eventLog)
                        : "[grey]No events yet...[/]")
                .Header("[bold blue] Event Log [/]")
                .Border(BoxBorder.Rounded);

            layout["Log"].Update(logPanel);

            ctx.UpdateTarget(layout);
            await Task.Delay(100);
        }
    });

await connection.StopAsync();

static string GetStateColor(int state)
{
    return state switch
    {
        0 => "red",
        1 => "yellow",
        2 => "green",
        _ => "grey"
    };
}

internal record EquipmentStateChangedEvent(
    Guid EquipmentId,
    string EquipmentName,
    int NewState,
    Guid? CurrentOrderId,
    DateTime ChangedAt);

internal record EquipmentStateInfo(
    Guid Id,
    string Name,
    int State,
    Guid? CurrentOrderId,
    DateTime ChangedAt);