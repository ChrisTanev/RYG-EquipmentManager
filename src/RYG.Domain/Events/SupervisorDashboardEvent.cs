using RYG.Domain.Enums;

namespace RYG.Domain.Events;

public record SupervisorDashboardEvent(
    IEnumerable<EquipmentWithOrdersInfo> EquipmentStates
);

public record EquipmentWithOrdersInfo(
    Guid EquipmentId,
    string EquipmentName,
    EquipmentState State,
    Guid? CurrentOrderId
);