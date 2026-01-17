using RYG.Shared.Enums;

namespace RYG.Domain.Entities;

public class Equipment
{
    private Equipment()
    {
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public EquipmentState State { get; private set; }
    public DateTime StateChangedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static Equipment Create(string name, EquipmentState initialState = EquipmentState.Red)
    {
        var now = DateTime.UtcNow;
        return new Equipment
        {
            Id = Guid.NewGuid(),
            Name = name,
            State = initialState,
            StateChangedAt = now,
            CreatedAt = now
        };
    }

    public void ChangeState(EquipmentState newState)
    {
        if (State == newState)
            return;

        State = newState;
        StateChangedAt = DateTime.UtcNow;
    }
}