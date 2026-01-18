namespace RYG.Domain.Tests;

public class EquipmentTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void Create_ShouldCreateEquipment_WithDefaultRedState()
    {
        // Arrange
        var name = _fixture.Create<string>();

        // Act
        var equipment = Equipment.Create(name);

        // Assert
        equipment.Id.Should().NotBeEmpty();
        equipment.Name.Should().Be(name);
        equipment.State.Should().Be(EquipmentState.Red);
        equipment.StateChangedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        equipment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_ShouldCreateEquipment_WithSpecifiedInitialState()
    {
        // Arrange
        var name = _fixture.Create<string>();
        var initialState = EquipmentState.Green;

        // Act
        var equipment = Equipment.Create(name, initialState);

        // Assert
        equipment.State.Should().Be(EquipmentState.Green);
    }

    [Fact]
    public void ChangeState_ShouldUpdateState_WhenDifferentState()
    {
        // Arrange
        var equipment = Equipment.Create(_fixture.Create<string>());
        var originalStateChangedAt = equipment.StateChangedAt;
        Thread.Sleep(10);

        // Act
        equipment.ChangeState(EquipmentState.Green);

        // Assert
        equipment.State.Should().Be(EquipmentState.Green);
        equipment.StateChangedAt.Should().BeAfter(originalStateChangedAt);
    }

    [Fact]
    public void ChangeState_ShouldNotUpdateTimestamp_WhenSameState()
    {
        // Arrange
        var equipment = Equipment.Create(_fixture.Create<string>());
        var originalStateChangedAt = equipment.StateChangedAt;

        // Act
        equipment.ChangeState(EquipmentState.Red);

        // Assert
        equipment.State.Should().Be(EquipmentState.Red);
        equipment.StateChangedAt.Should().Be(originalStateChangedAt);
    }

    [Theory]
    [InlineData(EquipmentState.Red, EquipmentState.Yellow)]
    [InlineData(EquipmentState.Yellow, EquipmentState.Green)]
    [InlineData(EquipmentState.Green, EquipmentState.Red)]
    public void ChangeState_ShouldTransitionBetweenAllStates(EquipmentState from, EquipmentState to)
    {
        // Arrange
        var equipment = Equipment.Create(_fixture.Create<string>(), from);

        // Act
        equipment.ChangeState(to);

        // Assert
        equipment.State.Should().Be(to);
    }
}