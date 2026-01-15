namespace RYG.Shared.Events;

public record OrderProcessingEvent(string EquipmentName, Guid OrderId, IEnumerable<ScheduledOrders> ScheduledOrders);