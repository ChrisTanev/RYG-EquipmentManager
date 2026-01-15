using RYG.Application.DTOs;

namespace RYG.Application.Services;

public interface IOrderService
{
    Task CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task ProcessQueuedOrdersAsync(CancellationToken cancellationToken = default);
}