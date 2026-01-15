using System.Text.Json;
using RYG.Domain.Interfaces;

namespace RYG.Functions.ServiceBusTriggers;

public class StateChangedSubscriber(
    ISignalRPublisher signalRPublisher,
    ILogger<StateChangedSubscriber> logger)
{
    [Function("StateChangedSubscriber")]
    [SignalROutput(HubName = "equipment", ConnectionStringSetting = "AzureSignalRConnectionString")]
    public async Task Run(
        [ServiceBusTrigger("equipment-events", "state-changed-subscription", Connection = "ServiceBusConnection")]
        string messageBody)
    {
        logger.LogInformation("Received state changed event: {Message}", messageBody);

        var stateChangedEvent = JsonSerializer.Deserialize<EquipmentStateChangedEvent>(messageBody) ??
                                throw new JsonException();

        logger.LogInformation(
            "Equipment {EquipmentId} ({EquipmentName}) state changed to {NewState}",
            stateChangedEvent.EquipmentId,
            stateChangedEvent.EquipmentName,
            stateChangedEvent.NewState);

        await signalRPublisher.SendToClientAsync(stateChangedEvent, "equipmentStateChanged");
    }
}