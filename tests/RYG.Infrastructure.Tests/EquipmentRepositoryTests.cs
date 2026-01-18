namespace RYG.Infrastructure.Tests;

public class EquipmentRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Fixture _fixture = new();
    private readonly EquipmentRepository _repository;

    public EquipmentRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new EquipmentRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_ShouldPersistEquipment()
    {
        // Arrange
        var equipment = Equipment.Create(_fixture.Create<string>());

        // Act
        await _repository.AddAsync(equipment);

        // Assert
        var savedEquipment = await _context.Equipment.FindAsync(equipment.Id);
        savedEquipment.Should().NotBeNull();
        savedEquipment.Name.Should().Be(equipment.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnEquipment_WhenExists()
    {
        // Arrange
        var equipment = Equipment.Create(_fixture.Create<string>());
        await _repository.AddAsync(equipment);

        // Act
        var result = await _repository.GetByIdAsync(equipment.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(equipment.Id);
    }


    [Fact]
    public async Task GetAllAsync_ShouldReturnAllEquipment()
    {
        // Arrange
        var equipment1 = Equipment.Create(_fixture.Create<string>());
        var equipment2 = Equipment.Create(_fixture.Create<string>());
        var equipment3 = Equipment.Create(_fixture.Create<string>());

        await _repository.AddAsync(equipment1);
        await _repository.AddAsync(equipment2);
        await _repository.AddAsync(equipment3);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistChanges()
    {
        // Arrange
        var equipment = Equipment.Create(_fixture.Create<string>());
        await _repository.AddAsync(equipment);
        equipment.ChangeState(EquipmentState.Green);

        // Act
        await _repository.UpdateAsync(equipment);

        // Assert
        var updatedEquipment = await _context.Equipment.FindAsync(equipment.Id);
        updatedEquipment!.State.Should().Be(EquipmentState.Green);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveEquipment()
    {
        // Arrange
        var equipment = Equipment.Create(_fixture.Create<string>());
        await _repository.AddAsync(equipment);

        // Act
        await _repository.DeleteAsync(equipment.Id);

        // Assert
        var deletedEquipment = await _context.Equipment.FindAsync(equipment.Id);
        deletedEquipment.Should().BeNull();
    }
}