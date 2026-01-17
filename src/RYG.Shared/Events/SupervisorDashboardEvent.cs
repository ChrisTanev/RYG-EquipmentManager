using RYG.Shared.Enums;

namespace RYG.Shared.Events;

public record EquipmentWithOrdersEvent(
    Guid EquipmentId,
    string EquipmentName,
    EquipmentState State,
    Guid OrderId
);