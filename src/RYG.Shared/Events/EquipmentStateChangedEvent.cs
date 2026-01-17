using RYG.Shared.Enums;

namespace RYG.Shared.Events;

public record EquipmentStateChangedEvent(
    Guid EquipmentId,
    string EquipmentName,
    EquipmentState NewState,
    DateTime ChangedAt
);