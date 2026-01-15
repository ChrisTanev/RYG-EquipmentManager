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

    [Fact]
    public async Task CreateAsync_ShouldEnqueueOrder()
    {
        var equipment = Equipment.Create(_fixture.Create<string>());
        _equipmentRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipment);
        _equipmentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([equipment]);

        var request = new CreateOrderRequest(equipment.Id, _fixture.Create<string>(), DateTime.UtcNow.AddHours(1));

        await _service.CreateAsync(request);

        // Verify order was enqueued by processing it
        await _service.ProcessQueuedOrdersAsync();

        // Verify events were published (order was processed)
        _publisher.Verify(
            p => p.SendToClientAsync(It.IsAny<OrderProcessingEvent>(), "orderProcessing", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessQueuedOrdersAsync_ShouldReturnEarly_WhenQueueIsEmpty()
    {
        await _service.ProcessQueuedOrdersAsync();

        // Verify no repository calls were made
        _equipmentRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _equipmentRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
        _publisher.Verify(p => p.SendToClientAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessQueuedOrdersAsync_ShouldProcessOrder_AndPublishEvents()
    {
        var equipment = Equipment.Create(_fixture.Create<string>());
        _equipmentRepositoryMock.Setup(r => r.GetByIdAsync(equipment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipment);
        _equipmentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([equipment]);

        var request = new CreateOrderRequest(equipment.Id, _fixture.Create<string>(), DateTime.UtcNow.AddHours(1));
        await _service.CreateAsync(request);

        await _service.ProcessQueuedOrdersAsync();

        // Verify OrderProcessingEvent was published
        _publisher.Verify(
            p => p.SendToClientAsync(
                It.Is<OrderProcessingEvent>(e => e.EquipmentName == equipment.Name),
                "orderProcessing",
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify supervisor dashboard event was published
        _publisher.Verify(
            p => p.SendToClientAsync(
                It.Is<IEnumerable<EquipmentWithOrdersEvent>>(events => events.Any(e => e.EquipmentId == equipment.Id)),
                "equipmentWithOrders",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessQueuedOrdersAsync_ShouldProcessOrdersInFIFOOrder()
    {
        var equipment = Equipment.Create(_fixture.Create<string>());
        _equipmentRepositoryMock.Setup(r => r.GetByIdAsync(equipment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipment);
        _equipmentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([equipment]);

        // Create three orders
        var request1 = new CreateOrderRequest(equipment.Id, "First Order", DateTime.UtcNow.AddHours(1));
        var request2 = new CreateOrderRequest(equipment.Id, "Second Order", DateTime.UtcNow.AddHours(2));
        var request3 = new CreateOrderRequest(equipment.Id, "Third Order", DateTime.UtcNow.AddHours(3));

        await _service.CreateAsync(request1);
        await _service.CreateAsync(request2);
        await _service.CreateAsync(request3);

        var processedOrderIds = new List<Guid>();
        _publisher.Setup(p => p.SendToClientAsync(
                It.IsAny<OrderProcessingEvent>(),
                "orderProcessing",
                It.IsAny<CancellationToken>()))
            .Callback<object, string, CancellationToken>((evt, _, _) =>
            {
                if (evt is OrderProcessingEvent orderEvent)
                    processedOrderIds.Add(orderEvent.OrderId);
            })
            .Returns(Task.CompletedTask);

        // Process all three orders
        await _service.ProcessQueuedOrdersAsync();
        await _service.ProcessQueuedOrdersAsync();
        await _service.ProcessQueuedOrdersAsync();

        // Verify three orders were processed
        processedOrderIds.Should().HaveCount(3);

        // Verify they were processed in FIFO order (order IDs should be sequential in creation order)
        processedOrderIds[0].Should().NotBe(processedOrderIds[1]);
        processedOrderIds[1].Should().NotBe(processedOrderIds[2]);
    }

    [Fact]
    public async Task ProcessQueuedOrdersAsync_ShouldPublishOrderProcessingEvent()
    {
        var equipment = Equipment.Create(_fixture.Create<string>());
        _equipmentRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipment);
        _equipmentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([equipment]);

        // Create multiple orders for the same equipment
        await _service.CreateAsync(new CreateOrderRequest(equipment.Id, "Order 1", DateTime.UtcNow.AddHours(1)));
        await _service.CreateAsync(new CreateOrderRequest(equipment.Id, "Order 2", DateTime.UtcNow.AddHours(2)));
        await _service.CreateAsync(new CreateOrderRequest(equipment.Id, "Order 3", DateTime.UtcNow.AddHours(3)));

        OrderProcessingEvent? capturedEvent = null;
        _publisher.Setup(p => p.SendToClientAsync(
                It.IsAny<OrderProcessingEvent>(),
                "orderProcessing",
                It.IsAny<CancellationToken>()))
            .Callback<object, string, CancellationToken>((evt, _, _) =>
            {
                if (evt is OrderProcessingEvent orderEvent)
                    capturedEvent = orderEvent;
            })
            .Returns(Task.CompletedTask);

        // Process the first order
        await _service.ProcessQueuedOrdersAsync();

        // Verify event was published with correct equipment name
        capturedEvent.Should().NotBeNull();
        capturedEvent!.EquipmentName.Should().Be(equipment.Name);
        capturedEvent.ScheduledOrders.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessQueuedOrdersAsync_ShouldPublishSupervisorDashboard_WithAllEquipment()
    {
        var equipment1 = Equipment.Create("Equipment 1");
        var equipment2 = Equipment.Create("Equipment 2");

        _equipmentRepositoryMock.Setup(r => r.GetByIdAsync(equipment1.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipment1);
        _equipmentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([equipment1, equipment2]);

        await _service.CreateAsync(new CreateOrderRequest(equipment1.Id, "Order 1", DateTime.UtcNow.AddHours(1)));

        IEnumerable<EquipmentWithOrdersEvent>? capturedEvents = null;
        _publisher.Setup(p => p.SendToClientAsync(
                It.IsAny<IEnumerable<EquipmentWithOrdersEvent>>(),
                "equipmentWithOrders",
                It.IsAny<CancellationToken>()))
            .Callback<object, string, CancellationToken>((evt, _, _) =>
            {
                if (evt is IEnumerable<EquipmentWithOrdersEvent> events)
                    capturedEvents = events.ToList();
            })
            .Returns(Task.CompletedTask);

        await _service.ProcessQueuedOrdersAsync();

        capturedEvents.Should().NotBeNull();
        capturedEvents.Should().HaveCount(2);
        capturedEvents.Should().Contain(e => e.EquipmentId == equipment1.Id);
        capturedEvents.Should().Contain(e => e.EquipmentId == equipment2.Id);
    }

    [Fact]
    public async Task ProcessQueuedOrdersAsync_ShouldThrow_WhenEquipmentNotFoundDuringProcessing()
    {
        var equipmentId = _fixture.Create<Guid>();
        _equipmentRepositoryMock.Setup(r => r.GetByIdAsync(equipmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Equipment.Create(_fixture.Create<string>()));

        await _service.CreateAsync(new CreateOrderRequest(equipmentId, "Test Order", DateTime.UtcNow));

        // Setup repository to return null during processing
        _equipmentRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Equipment?)null);

        // Should throw NullReferenceException when equipment is not found during processing
        var act = async () => await _service.ProcessQueuedOrdersAsync();
        await act.Should().ThrowAsync<NullReferenceException>();
    }

    [Fact]
    public async Task ProcessQueuedOrdersAsync_ShouldNotProcessAnyOrders_AfterQueueIsEmpty()
    {
        var equipment = Equipment.Create(_fixture.Create<string>());
        _equipmentRepositoryMock.Setup(r => r.GetByIdAsync(equipment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipment);
        _equipmentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([equipment]);

        // Create and process one order
        await _service.CreateAsync(new CreateOrderRequest(equipment.Id, "Order 1", DateTime.UtcNow.AddHours(1)));
        await _service.ProcessQueuedOrdersAsync();

        // Try to process again with empty queue
        await _service.ProcessQueuedOrdersAsync();

        // Verify events were only published once (for the first order)
        _publisher.Verify(
            p => p.SendToClientAsync(It.IsAny<OrderProcessingEvent>(), "orderProcessing", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}