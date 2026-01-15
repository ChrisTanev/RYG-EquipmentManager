namespace RYG.Application.Tests;

public class OrderServiceTests
{
    private readonly Mock<IEquipmentRepository> _equipmentRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly Mock<ILogger<OrderService>> _loggerMock = new();
    private readonly Mock<ISignalRPublisher> _publisher = new();
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        _service = new OrderService(
            _equipmentRepositoryMock.Object,
            _publisher.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateOrder_AndReturnDto()
    {
        var equipment = Equipment.Create(_fixture.Create<string>());
        _equipmentRepositoryMock.Setup(r => r.GetByIdAsync(equipment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipment);

        var request = new CreateOrderRequest(equipment.Id, _fixture.Create<string>(), DateTime.UtcNow.AddHours(1));

        var func = async () => await _service.CreateAsync(request);
        await func.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowException_WhenEquipmentNotFound()
    {
        var equipmentId = _fixture.Create<Guid>();
        _equipmentRepositoryMock.Setup(r => r.GetByIdAsync(equipmentId, It.IsAny<CancellationToken>()))!
            .ReturnsAsync((Equipment?)null);

        var request = new CreateOrderRequest(equipmentId, _fixture.Create<string>(), DateTime.UtcNow);

        var act = async () => await _service.CreateAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage($"Equipment with ID {equipmentId} not found");
    }
}