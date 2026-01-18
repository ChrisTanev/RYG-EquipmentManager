namespace RYG.Application.Tests;

public class ValidatorTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public async Task CreateEquipmentValidator_ShouldFail_WhenNameIsEmpty()
    {
        // Arrange
        var validator = new CreateEquipmentValidator();
        var request = new CreateEquipmentRequest(string.Empty);

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task CreateEquipmentValidator_ShouldFail_WhenNameExceeds100Characters()
    {
        // Arrange
        var validator = new CreateEquipmentValidator();
        var longName = new string('a', 101);
        var request = new CreateEquipmentRequest(longName);

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task CreateEquipmentValidator_ShouldPass_WhenNameIsValid()
    {
        // Arrange
        var validator = new CreateEquipmentValidator();
        var request = new CreateEquipmentRequest(_fixture.Create<string>());

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(EquipmentState.Red)]
    [InlineData(EquipmentState.Yellow)]
    [InlineData(EquipmentState.Green)]
    public async Task ChangeStateValidator_ShouldPass_ForValidStates(EquipmentState state)
    {
        // Arrange
        var validator = new ChangeStateValidator();
        var request = new ChangeStateRequest(state);

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ChangeStateValidator_ShouldFail_ForInvalidState()
    {
        // Arrange
        var validator = new ChangeStateValidator();
        var request = new ChangeStateRequest((EquipmentState)999);

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateOrderValidator_ShouldFail_WhenEquipmentIdIsEmpty()
    {
        // Arrange
        var validator = new CreateOrderValidator();
        var request = new CreateOrderRequest(Guid.Empty, "Test Order", DateTime.UtcNow);

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EquipmentId");
    }

    [Fact]
    public async Task CreateOrderValidator_ShouldFail_WhenDescriptionIsEmpty()
    {
        // Arrange
        var validator = new CreateOrderValidator();
        var request = new CreateOrderRequest(_fixture.Create<Guid>(), string.Empty, DateTime.UtcNow);

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task CreateOrderValidator_ShouldFail_WhenDescriptionExceeds500Characters()
    {
        // Arrange
        var validator = new CreateOrderValidator();
        var longDescription = new string('a', 501);
        var request = new CreateOrderRequest(_fixture.Create<Guid>(), longDescription, DateTime.UtcNow);

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task CreateOrderValidator_ShouldPass_WhenRequestIsValid()
    {
        // Arrange
        var validator = new CreateOrderValidator();
        var request = new CreateOrderRequest(_fixture.Create<Guid>(), "Test Order", DateTime.UtcNow);

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}