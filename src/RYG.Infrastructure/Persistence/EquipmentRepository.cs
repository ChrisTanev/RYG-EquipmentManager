using RYG.Domain.Exceptions;

namespace RYG.Infrastructure.Persistence;

public class EquipmentRepository(AppDbContext context) : IEquipmentRepository
{
    public async Task<Equipment> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Equipment.FindAsync([id], cancellationToken) ?? throw new EquipmentNotFoundException(id);
    }

    public async Task<IEnumerable<Equipment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.Equipment.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Equipment equipment, CancellationToken cancellationToken = default)
    {
        await context.Equipment.AddAsync(equipment, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default)
    {
        context.Equipment.Update(equipment);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var equipment = await GetByIdAsync(id, cancellationToken);
        if (equipment is not null)
        {
            context.Equipment.Remove(equipment);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}