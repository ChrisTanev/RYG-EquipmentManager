using System.Text.Json;

namespace RYG.Functions.ServiceBusTriggers;

public class StateChangedSubscriber(ILogger<StateChangedSubscriber> logger)
{
    [Function("StateChangedSubscriber")]
    [SignalROutput(HubName = "equipment", ConnectionStringSetting = "AzureSignalRConnectionString")]
    public SignalRMessageAction Run(
        [ServiceBusTrigger("equipment-events", "state-changed-subscription", Connection = "ServiceBusConnection")]
        string messageBody)
    {
        logger.LogInformation("Received state changed event: {Message}", messageBody);

        var stateChangedEvent = JsonSerializer.Deserialize<EquipmentStateChangedEvent>(messageBody);

        if (stateChangedEvent is null) // TODO throw instead?
        {
            logger.LogWarning("Failed to deserialize state changed event");
            return new SignalRMessageAction("equipmentStateChanged")
            {
                Arguments = [messageBody]
            };
        }

        logger.LogInformation(
            "Equipment {EquipmentId} ({EquipmentName}) state changed to {NewState}",
            stateChangedEvent.EquipmentId,
            stateChangedEvent.EquipmentName,
            stateChangedEvent.NewState);

        return new SignalRMessageAction("equipmentStateChanged")
        {
            Arguments = [stateChangedEvent]
        };
    }
}