namespace RYG.Shared.Events;

public record ScheduledOrders(string EquipmentName, Guid EquipmentId, Guid OrderId, DateTime ScheduledAt);