using System.Collections.Concurrent;
using RYG.Application.DTOs;

namespace RYG.Application.Services;

public class OrderService(
    IEquipmentRepository equipmentRepository,
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

        await PublishSupervisorDashboardAsync(cancellationToken);

        // TODO publish to Operators Dashboard
        logger.LogInformation("Completed order {OrderId}", order.Id);
    }

    private async Task PublishSupervisorDashboardAsync(CancellationToken cancellationToken)
    {
        var allEquipment = await equipmentRepository.GetAllAsync(cancellationToken);
        var equipmentWithOrders = new List<EquipmentWithOrdersInfo>();

        foreach (var eq in allEquipment)
            equipmentWithOrders.Add(new EquipmentWithOrdersInfo(
                eq.Id,
                eq.Name,
                eq.State,
                eq.CurrentOrderId));

        var dashboardEvent = new SupervisorDashboardEvent(equipmentWithOrders);
        // TODO Push to signalR
    }
}