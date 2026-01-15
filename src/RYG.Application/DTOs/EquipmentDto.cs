namespace RYG.Application.DTOs;

public record EquipmentDto(
    Guid Id,
    string Name,
    EquipmentState State,
    Guid? CurrentOrderId,
    DateTime StateChangedAt,
    DateTime CreatedAt // TODO
);