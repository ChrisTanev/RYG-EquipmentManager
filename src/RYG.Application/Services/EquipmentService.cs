using AutoMapper;
using RYG.Application.DTOs;
using RYG.Domain.Exceptions;

namespace RYG.Application.Services;

public class EquipmentService(
    IEquipmentRepository repository,
    IEventPublisher eventPublisher,
    IMapper mapper,
    ILogger<EquipmentService> logger) : IEquipmentService
{
    public async Task<EquipmentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var equipment = await repository.GetByIdAsync(id, cancellationToken) ?? throw new EquipmentNotFoundException(id);
        return mapper.Map<EquipmentDto>(equipment);
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

    public async Task<EquipmentDto> ChangeStateAsync(Guid id, ChangeStateRequest request,
        CancellationToken cancellationToken = default)
    {
        var equipment = await repository.GetByIdAsync(id, cancellationToken);

        equipment.ChangeState(request.State);
        await repository.UpdateAsync(equipment, cancellationToken);

        var stateChangedEvent = new EquipmentStateChangedEvent(
            equipment.Id,
            equipment.Name,
            equipment.State,
            equipment.CurrentOrderId,
            equipment.StateChangedAt
        );

        await eventPublisher.PublishAsync(stateChangedEvent, cancellationToken);

        logger.LogInformation(
            "Equipment {EquipmentId} state changed to {NewState}",
            equipment.Id, equipment.State);

        return mapper.Map<EquipmentDto>(equipment);
    }
}