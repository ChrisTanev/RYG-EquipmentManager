namespace RYG.Application.Tests;

public class ValidatorTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public async Task CreateEquipmentValidator_ShouldFail_WhenNameIsEmpty()
    {
        var validator = new CreateEquipmentValidator();
        var request = new CreateEquipmentRequest(string.Empty);

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task CreateEquipmentValidator_ShouldFail_WhenNameExceeds100Characters()
    {
        var validator = new CreateEquipmentValidator();
        var longName = new string('a', 101);
        var request = new CreateEquipmentRequest(longName);

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task CreateEquipmentValidator_ShouldPass_WhenNameIsValid()
    {
        var validator = new CreateEquipmentValidator();
        var request = new CreateEquipmentRequest(_fixture.Create<string>());

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(EquipmentState.Red)]
    [InlineData(EquipmentState.Yellow)]
    [InlineData(EquipmentState.Green)]
    public async Task ChangeStateValidator_ShouldPass_ForValidStates(EquipmentState state)
    {
        var validator = new ChangeStateValidator();
        var request = new ChangeStateRequest(state);

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ChangeStateValidator_ShouldFail_ForInvalidState()
    {
        var validator = new ChangeStateValidator();
        var request = new ChangeStateRequest((EquipmentState)999);

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateOrderValidator_ShouldFail_WhenEquipmentIdIsEmpty()
    {
        var validator = new CreateOrderValidator();
        var request = new CreateOrderRequest(Guid.Empty, "Test Order", DateTime.UtcNow);

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EquipmentId");
    }

    [Fact]
    public async Task CreateOrderValidator_ShouldFail_WhenDescriptionIsEmpty()
    {
        var validator = new CreateOrderValidator();
        var request = new CreateOrderRequest(_fixture.Create<Guid>(), string.Empty, DateTime.UtcNow);

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task CreateOrderValidator_ShouldFail_WhenDescriptionExceeds500Characters()
    {
        var validator = new CreateOrderValidator();
        var longDescription = new string('a', 501);
        var request = new CreateOrderRequest(_fixture.Create<Guid>(), longDescription, DateTime.UtcNow);

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task CreateOrderValidator_ShouldPass_WhenRequestIsValid()
    {
        var validator = new CreateOrderValidator();
        var request = new CreateOrderRequest(_fixture.Create<Guid>(), "Test Order", DateTime.UtcNow);

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }
}