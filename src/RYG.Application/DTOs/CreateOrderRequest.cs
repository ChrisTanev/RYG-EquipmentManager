namespace RYG.Application.DTOs;

public record CreateOrderRequest(
    Guid EquipmentId,
    string Description,
    DateTime ScheduledAt);