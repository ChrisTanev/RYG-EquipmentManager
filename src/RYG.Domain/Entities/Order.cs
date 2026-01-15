namespace RYG.Domain.Entities;

public record Order(Guid EquipmentId)
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid EquipmentId { get; private set; } = EquipmentId;
    public DateTime ScheduledAt { get; private set; } = DateTime.UtcNow;
}