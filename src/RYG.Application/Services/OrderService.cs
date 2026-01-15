using System.Collections.Concurrent;
using RYG.Application.DTOs;
using RYG.Shared.Events;

namespace RYG.Application.Services;

// TODO should be backed by persistent storage in real application
public class OrderService(
    IEquipmentRepository equipmentRepository,
    ISignalRPublisher signalRPublisher,
    ILogger<OrderService> logger) : IOrderService
{
    private readonly ConcurrentQueue<Order> _orderQueue = new();

    public async Task CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var equipment = await equipmentRepository.GetByIdAsync(request.EquipmentId, cancellationToken);
        if (equipment is null) throw new ArgumentException($"Equipment with ID {request.EquipmentId} not found");

        var order = new Order(request.EquipmentId);
        _orderQueue.Enqueue(order);

        logger.LogInformation(
            "Created order {OrderId} for equipment {EquipmentId} scheduled at {ScheduledAt}",
            order.Id, order.EquipmentId, order.ScheduledAt);
    }

    public async Task ProcessQueuedOrdersAsync(CancellationToken cancellationToken = default)
    {
        if (!_orderQueue.TryDequeue(out var order))
        {
            logger.LogError("No order queue available");
            return;
        }

        var equipment = await equipmentRepository.GetByIdAsync(order.EquipmentId, cancellationToken);

        logger.LogInformation("Processing order {OrderId} for equipment {EquipmentId}",
            order.Id, order.EquipmentId);

        await PublishScheduleOrdersAsync(equipment, order, cancellationToken);

        await PublishSupervisorProdStatesForEquiopmentAsync(cancellationToken);

        logger.LogInformation("Completed order {OrderId}", order.Id);
    }

    // Stretch goal: publish currently performing + scheduled orders
    private async Task PublishScheduleOrdersAsync(Equipment equipment, Order order, CancellationToken cancellationToken)
    {
        var allScheduledOrdersForEquipment = _orderQueue.Where(f => f.EquipmentId == equipment.Id);
        var scheduledOrders = allScheduledOrdersForEquipment
            .Select(a =>
                new ScheduledOrders("", a.EquipmentId, order.Id, a.ScheduledAt))
            .Where(o => o.OrderId != order.Id)
            .ToList();

        var orderProcessingEvent = new OrderProcessingEvent(equipment.Name, order.Id, scheduledOrders);

        await signalRPublisher.SendToClientAsync(orderProcessingEvent, "orderProcessing", cancellationToken);
    }

    // Stretch goal: publish production states for supervisor view
    private async Task PublishSupervisorProdStatesForEquiopmentAsync(CancellationToken cancellationToken)
    {
        var allEquipment = await equipmentRepository.GetAllAsync(cancellationToken);
        var equipmentWithOrders = new List<EquipmentWithOrdersEvent>();

        foreach (var eq in allEquipment)
            equipmentWithOrders.Add(new EquipmentWithOrdersEvent(
                eq.Id,
                eq.Name,
                eq.State,
                eq.CurrentOrderId));

        await signalRPublisher.SendToClientAsync(equipmentWithOrders, "equipmentWithOrders", cancellationToken);
    }
}