namespace RYG.Application.Tests;

public class EquipmentServiceTests
{
    private readonly Mock<IEventPublisher> _eventPublisherMock = new();
    private readonly Fixture _fixture = new();
    private readonly Mock<ILogger<EquipmentService>> _loggerMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<IEquipmentRepository> _repositoryMock = new();
    private readonly EquipmentService _service;
    private readonly Mock<ISignalRPublisher> _signalRPublisherMock = new();

    public EquipmentServiceTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        var mapper = config.CreateMapper();

        _service = new EquipmentService(_repositoryMock.Object, _eventPublisherMock.Object, mapper,
            _signalRPublisherMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateEquipment_AndReturnDto()
    {
        var request = new CreateEquipmentRequest(_fixture.Create<string>(), EquipmentState.Yellow);

        var result = await _service.CreateAsync(request);

        result.Should().NotBeNull();
        result.Name.Should().Be(request.Name);
        result.State.Should().Be(EquipmentState.Yellow);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Equipment>(), It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task ChangeStateAsync_ShouldPublishEvent_WhenStateChanges()
    {
        var equipment = Equipment.Create(_fixture.Create<string>());
        var id = equipment.Id;

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipment);

        var request = new ChangeStateRequest(EquipmentState.Green);

        var result = await _service.ChangeStateAsync(id, request);

        result.Should().NotBeNull();
        result.State.Should().Be(EquipmentState.Green);
        _eventPublisherMock.Verify(
            e => e.PublishAsync(It.IsAny<EquipmentStateChangedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}