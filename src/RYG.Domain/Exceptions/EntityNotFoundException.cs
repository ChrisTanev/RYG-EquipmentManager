namespace RYG.Domain.Exceptions;

public class EquipmentNotFoundException(Guid id) : Exception($"Equipment with ID {id} not found")
{
    public Guid EquipmentId { get; } = id;
}