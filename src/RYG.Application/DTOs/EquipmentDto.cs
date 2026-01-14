namespace RYG.Application.DTOs;

public record EquipmentDto(
    Guid Id,
    string Name,
    EquipmentState State,
    DateTime StateChangedAt,
    DateTime CreatedAt
);