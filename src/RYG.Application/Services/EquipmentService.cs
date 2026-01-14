using AutoMapper;
using RYG.Application.DTOs;

namespace RYG.Application.Services;

public class EquipmentService(
    IEquipmentRepository repository,
    IEventPublisher eventPublisher,
    IMapper mapper,
    ILogger<EquipmentService> logger) : IEquipmentService
{
    public async Task<EquipmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var equipment = await repository.GetByIdAsync(id, cancellationToken);
        return equipment is null ? null : mapper.Map<EquipmentDto>(equipment); // TODO throw instead of returning null
    }

    public async Task<IEnumerable<EquipmentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var equipment = await repository.GetAllAsync(cancellationToken);
        return mapper.Map<IEnumerable<EquipmentDto>>(equipment);
    }

    public async Task<EquipmentDto> CreateAsync(CreateEquipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var equipment = Equipment.Create(request.Name, request.InitialState);
        await repository.AddAsync(equipment, cancellationToken);

        logger.LogInformation("Created equipment {EquipmentId} with name {EquipmentName} and state {State}",
            equipment.Id, equipment.Name, equipment.State);

        return mapper.Map<EquipmentDto>(equipment);
    }

    public async Task<EquipmentDto?> UpdateAsync(Guid id, UpdateEquipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var equipment = await repository.GetByIdAsync(id, cancellationToken);
        if (equipment is null) return null; // TODO throw instead of returning null


        equipment.UpdateName(request.Name);
        await repository.UpdateAsync(equipment, cancellationToken);

        logger.LogInformation("Updated equipment {EquipmentId} name to {EquipmentName}", equipment.Id, equipment.Name);

        return mapper.Map<EquipmentDto>(equipment);
    }

    public async Task<EquipmentDto?> ChangeStateAsync(Guid id, ChangeStateRequest request,
        CancellationToken cancellationToken = default)
    {
        var equipment = await repository.GetByIdAsync(id, cancellationToken);
        if (equipment is null)
            return null; // TODO throw instead of returning null

        var previousState = equipment.State;
        equipment.ChangeState(request.State);

        if (previousState != equipment.State)
        {
            await repository.UpdateAsync(equipment, cancellationToken);

            var stateChangedEvent = new EquipmentStateChangedEvent(
                equipment.Id,
                equipment.Name,
                equipment.State,
                equipment.StateChangedAt
            );

            await eventPublisher.PublishAsync(stateChangedEvent, cancellationToken);

            logger.LogInformation(
                "Equipment {EquipmentId} state changed from {PreviousState} to {NewState}",
                equipment.Id, previousState, equipment.State);
        }

        return mapper.Map<EquipmentDto>(equipment);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var equipment = await repository.GetByIdAsync(id, cancellationToken);
        if (equipment is null)
            return false;

        await repository.DeleteAsync(id, cancellationToken);

        logger.LogInformation("Deleted equipment {EquipmentId}", id);

        return true;
    }
}