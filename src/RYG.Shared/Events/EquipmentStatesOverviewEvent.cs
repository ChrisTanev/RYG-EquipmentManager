using RYG.Shared.Enums;

namespace RYG.Shared.Events;

public record EquipmentStatesOverviewEvent(string Name, EquipmentState State);