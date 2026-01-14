using RYG.Domain.Entities;

namespace RYG.Domain.Interfaces;

public interface IEquipmentRepository
{
    Task<Equipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Equipment>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Equipment equipment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}