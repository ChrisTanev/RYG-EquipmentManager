namespace RYG.Application.DTOs;

public record CreateEquipmentRequest(string Name, EquipmentState InitialState = EquipmentState.Red);