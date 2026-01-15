using RYG.Shared.Enums;

namespace RYG.Shared.Events;

public record EquipmentStateChangedEvent(
    Guid EquipmentId,
    string EquipmentName,
    EquipmentState NewState,
    Guid? CurrentOrderId,
    DateTime ChangedAt
);