namespace RYG.Domain.Tests;

public class EquipmentTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void Create_ShouldCreateEquipment_WithDefaultRedState()
    {
        var name = _fixture.Create<string>();

        var equipment = Equipment.Create(name);

        equipment.Id.Should().NotBeEmpty();
        equipment.Name.Should().Be(name);
        equipment.State.Should().Be(EquipmentState.Red);
        equipment.StateChangedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        equipment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_ShouldCreateEquipment_WithSpecifiedInitialState()
    {
        var name = _fixture.Create<string>();
        var initialState = EquipmentState.Green;

        var equipment = Equipment.Create(name, initialState);

        equipment.State.Should().Be(EquipmentState.Green);
    }

    [Fact]
    public void ChangeState_ShouldUpdateState_WhenDifferentState()
    {
        var equipment = Equipment.Create(_fixture.Create<string>());
        var originalStateChangedAt = equipment.StateChangedAt;

        Thread.Sleep(10);

        equipment.ChangeState(EquipmentState.Green);

        equipment.State.Should().Be(EquipmentState.Green);
        equipment.StateChangedAt.Should().BeAfter(originalStateChangedAt);
    }

    [Fact]
    public void ChangeState_ShouldNotUpdateTimestamp_WhenSameState()
    {
        var equipment = Equipment.Create(_fixture.Create<string>());
        var originalStateChangedAt = equipment.StateChangedAt;

        equipment.ChangeState(EquipmentState.Red);

        equipment.State.Should().Be(EquipmentState.Red);
        equipment.StateChangedAt.Should().Be(originalStateChangedAt);
    }

    [Theory]
    [InlineData(EquipmentState.Red, EquipmentState.Yellow)]
    [InlineData(EquipmentState.Yellow, EquipmentState.Green)]
    [InlineData(EquipmentState.Green, EquipmentState.Red)]
    public void ChangeState_ShouldTransitionBetweenAllStates(EquipmentState from, EquipmentState to)
    {
        var equipment = Equipment.Create(_fixture.Create<string>(), from);

        equipment.ChangeState(to);

        equipment.State.Should().Be(to);
    }
}