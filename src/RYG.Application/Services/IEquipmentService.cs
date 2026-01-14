using RYG.Application.DTOs;

namespace RYG.Application.Services;

public interface IEquipmentService
{
    Task<EquipmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<EquipmentDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<EquipmentDto> CreateAsync(CreateEquipmentRequest request, CancellationToken cancellationToken = default);

    Task<EquipmentDto?> UpdateAsync(Guid id, UpdateEquipmentRequest request,
        CancellationToken cancellationToken = default);

    Task<EquipmentDto?> ChangeStateAsync(Guid id, ChangeStateRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}