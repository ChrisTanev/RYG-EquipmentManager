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
        var equipment = Equipment.Create(_fixture.Create<string>());

        await _repository.AddAsync(equipment);

        var savedEquipment = await _context.Equipment.FindAsync(equipment.Id);
        savedEquipment.Should().NotBeNull();
        savedEquipment.Name.Should().Be(equipment.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnEquipment_WhenExists()
    {
        var equipment = Equipment.Create(_fixture.Create<string>());
        await _repository.AddAsync(equipment);

        var result = await _repository.GetByIdAsync(equipment.Id);

        result.Should().NotBeNull();
        result.Id.Should().Be(equipment.Id);
    }


    [Fact]
    public async Task GetAllAsync_ShouldReturnAllEquipment()
    {
        var equipment1 = Equipment.Create(_fixture.Create<string>());
        var equipment2 = Equipment.Create(_fixture.Create<string>());
        var equipment3 = Equipment.Create(_fixture.Create<string>());

        await _repository.AddAsync(equipment1);
        await _repository.AddAsync(equipment2);
        await _repository.AddAsync(equipment3);

        var result = await _repository.GetAllAsync();

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistChanges()
    {
        var equipment = Equipment.Create(_fixture.Create<string>());
        await _repository.AddAsync(equipment);

        equipment.ChangeState(EquipmentState.Green);
        await _repository.UpdateAsync(equipment);

        var updatedEquipment = await _context.Equipment.FindAsync(equipment.Id);
        updatedEquipment!.State.Should().Be(EquipmentState.Green);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveEquipment()
    {
        var equipment = Equipment.Create(_fixture.Create<string>());
        await _repository.AddAsync(equipment);

        await _repository.DeleteAsync(equipment.Id);

        var deletedEquipment = await _context.Equipment.FindAsync(equipment.Id);
        deletedEquipment.Should().BeNull();
    }
}