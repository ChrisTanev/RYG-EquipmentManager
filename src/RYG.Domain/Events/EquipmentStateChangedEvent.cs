using RYG.Domain.Enums;

namespace RYG.Domain.Events;

public record EquipmentStateChangedEvent(
    Guid EquipmentId,
    string EquipmentName,
    EquipmentState NewState,
    Guid? CurrentOrderId,
    DateTime ChangedAt
);