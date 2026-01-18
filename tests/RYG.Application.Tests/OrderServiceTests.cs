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
        // Arrange
        var equipment = Equipment.Create(_fixture.Create<string>());
        _equipmentRepositoryMock.Setup(r => r.GetByIdAsync(equipment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipment);

        var request = new CreateOrderRequest(equipment.Id, _fixture.Create<string>(), DateTime.UtcNow.AddHours(1));

        // Act
        var func = async () => await _service.CreateAsync(request);

        // Assert
        await func.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowException_WhenEquipmentNotFound()
    {
        // Arrange
        var equipmentId = _fixture.Create<Guid>();
        _equipmentRepositoryMock.Setup(r => r.GetByIdAsync(equipmentId, It.IsAny<CancellationToken>()))!
            .ReturnsAsync((Equipment?)null);

        var request = new CreateOrderRequest(equipmentId, _fixture.Create<string>(), DateTime.UtcNow);

        // Act
        var act = async () => await _service.CreateAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage($"Equipment with ID {equipmentId} not found");
    }

    [Fact]
    public async Task CreateAsync_ShouldEnqueueOrder()
    {
        // Arrange
        var equipment = Equipment.Create(_fixture.Create<string>());
        _equipmentRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipment);
        _equipmentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([equipment]);

        var request = new CreateOrderRequest(equipment.Id, _fixture.Create<string>(), DateTime.UtcNow.AddHours(1));

        // Act
        await _service.CreateAsync(request);
        await _service.ProcessQueuedOrdersAsync();

        // Assert
        _publisher.Verify(
            p => p.SendToGroupAsync(It.IsAny<OrderProcessingEvent>(), "orderProcessing", "operators", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessQueuedOrdersAsync_ShouldReturnEarly_WhenQueueIsEmpty()
    {
        // Arrange - empty queue (no setup needed)

        // Act
        await _service.ProcessQueuedOrdersAsync();

        // Assert
        _equipmentRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _equipmentRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
        _publisher.Verify(p => p.SendToGroupAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessQueuedOrdersAsync_ShouldProcessOrder_AndPublishEvents()
    {
        // Arrange
        var equipment = Equipment.Create(_fixture.Create<string>());
        _equipmentRepositoryMock.Setup(r => r.GetByIdAsync(equipment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipment);
        _equipmentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([equipment]);

        var request = new CreateOrderRequest(equipment.Id, _fixture.Create<string>(), DateTime.UtcNow.AddHours(1));
        await _service.CreateAsync(request);

        // Act
        await _service.ProcessQueuedOrdersAsync();

        // Assert
        _publisher.Verify(
            p => p.SendToGroupAsync(
                It.Is<OrderProcessingEvent>(e => e.EquipmentName == equipment.Name),
                "orderProcessing",
                "operators",
                It.IsAny<CancellationToken>()),
            Times.Once);

        _publisher.Verify(
            p => p.SendToGroupAsync(
                It.Is<List<EquipmentWithOrdersEvent>>(events => events.Any(e => e.EquipmentId == equipment.Id)),
                "equipmentWithOrders",
                "supervisors",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessQueuedOrdersAsync_ShouldProcessOrdersInFIFOOrder()
    {
        // Arrange
        var equipment = Equipment.Create(_fixture.Create<string>());
        _equipmentRepositoryMock.Setup(r => r.GetByIdAsync(equipment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipment);
        _equipmentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([equipment]);

        var request1 = new CreateOrderRequest(equipment.Id, "First Order", DateTime.UtcNow.AddHours(1));
        var request2 = new CreateOrderRequest(equipment.Id, "Second Order", DateTime.UtcNow.AddHours(2));
        var request3 = new CreateOrderRequest(equipment.Id, "Third Order", DateTime.UtcNow.AddHours(3));

        await _service.CreateAsync(request1);
        await _service.CreateAsync(request2);
        await _service.CreateAsync(request3);

        var processedOrderIds = new List<Guid>();
        _publisher.Setup(p => p.SendToGroupAsync(
                It.IsAny<OrderProcessingEvent>(),
                "orderProcessing",
                "operators",
                It.IsAny<CancellationToken>()))
            .Callback<object, string, string, CancellationToken>((evt, _, _, _) =>
            {
                if (evt is OrderProcessingEvent orderEvent)
                    processedOrderIds.Add(orderEvent.OrderId);
            })
            .Returns(Task.CompletedTask);

        // Act
        await _service.ProcessQueuedOrdersAsync();
        await _service.ProcessQueuedOrdersAsync();
        await _service.ProcessQueuedOrdersAsync();

        // Assert
        processedOrderIds.Should().HaveCount(3);
        processedOrderIds[0].Should().NotBe(processedOrderIds[1]);
        processedOrderIds[1].Should().NotBe(processedOrderIds[2]);
    }

    [Fact]
    public async Task ProcessQueuedOrdersAsync_ShouldPublishOrderProcessingEvent()
    {
        // Arrange
        var equipment = Equipment.Create(_fixture.Create<string>());
        _equipmentRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipment);
        _equipmentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([equipment]);

        await _service.CreateAsync(new CreateOrderRequest(equipment.Id, "Order 1", DateTime.UtcNow.AddHours(1)));
        await _service.CreateAsync(new CreateOrderRequest(equipment.Id, "Order 2", DateTime.UtcNow.AddHours(2)));
        await _service.CreateAsync(new CreateOrderRequest(equipment.Id, "Order 3", DateTime.UtcNow.AddHours(3)));

        OrderProcessingEvent? capturedEvent = null;
        _publisher.Setup(p => p.SendToGroupAsync(
                It.IsAny<OrderProcessingEvent>(),
                "orderProcessing",
                "operators",
                It.IsAny<CancellationToken>()))
            .Callback<object, string, string, CancellationToken>((evt, _, _, _) =>
            {
                if (evt is OrderProcessingEvent orderEvent)
                    capturedEvent = orderEvent;
            })
            .Returns(Task.CompletedTask);

        // Act
        await _service.ProcessQueuedOrdersAsync();

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.EquipmentName.Should().Be(equipment.Name);
        capturedEvent.ScheduledOrders.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessQueuedOrdersAsync_ShouldPublishSupervisorDashboard_WithAllEquipment()
    {
        // Arrange
        var equipment1 = Equipment.Create("Equipment 1");
        var equipment2 = Equipment.Create("Equipment 2");

        _equipmentRepositoryMock.Setup(r => r.GetByIdAsync(equipment1.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipment1);
        _equipmentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([equipment1, equipment2]);

        await _service.CreateAsync(new CreateOrderRequest(equipment1.Id, "Order 1", DateTime.UtcNow.AddHours(1)));

        IEnumerable<EquipmentWithOrdersEvent>? capturedEvents = null;
        _publisher.Setup(p => p.SendToGroupAsync(
                It.IsAny<List<EquipmentWithOrdersEvent>>(),
                "equipmentWithOrders",
                "supervisors",
                It.IsAny<CancellationToken>()))
            .Callback<object, string, string, CancellationToken>((evt, _, _, _) =>
            {
                if (evt is IEnumerable<EquipmentWithOrdersEvent> events)
                    capturedEvents = events.ToList();
            })
            .Returns(Task.CompletedTask);

        // Act
        await _service.ProcessQueuedOrdersAsync();

        // Assert
        capturedEvents.Should().NotBeNull();
        capturedEvents.Should().HaveCount(2);
        capturedEvents.Should().Contain(e => e.EquipmentId == equipment1.Id);
        capturedEvents.Should().Contain(e => e.EquipmentId == equipment2.Id);
    }

    [Fact]
    public async Task ProcessQueuedOrdersAsync_ShouldThrow_WhenEquipmentNotFoundDuringProcessing()
    {
        // Arrange
        var equipmentId = _fixture.Create<Guid>();
        _equipmentRepositoryMock.Setup(r => r.GetByIdAsync(equipmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Equipment.Create(_fixture.Create<string>()));

        await _service.CreateAsync(new CreateOrderRequest(equipmentId, "Test Order", DateTime.UtcNow));

        _equipmentRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Equipment?)null);

        // Act
        var act = async () => await _service.ProcessQueuedOrdersAsync();

        // Assert
        await act.Should().ThrowAsync<NullReferenceException>();
    }

    [Fact]
    public async Task ProcessQueuedOrdersAsync_ShouldNotProcessAnyOrders_AfterQueueIsEmpty()
    {
        // Arrange
        var equipment = Equipment.Create(_fixture.Create<string>());
        _equipmentRepositoryMock.Setup(r => r.GetByIdAsync(equipment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipment);
        _equipmentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([equipment]);

        await _service.CreateAsync(new CreateOrderRequest(equipment.Id, "Order 1", DateTime.UtcNow.AddHours(1)));
        await _service.ProcessQueuedOrdersAsync();

        // Act
        await _service.ProcessQueuedOrdersAsync();

        // Assert
        _publisher.Verify(
            p => p.SendToGroupAsync(It.IsAny<OrderProcessingEvent>(), "orderProcessing", "operators", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}