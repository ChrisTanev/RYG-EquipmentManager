using Microsoft.AspNetCore.SignalR;
using RYG.Infrastructure.Hubs;

namespace RYG.Infrastructure.Messaging;

public class SignalRPublisher(IHubContext<EquipmentHub> hubContext) : ISignalRPublisher
{
    public async Task SendToClientAsync<TEvent>(TEvent @event, string methodName,
        CancellationToken cancellationToken = default)
        where TEvent : class
    {
        await hubContext.Clients.All.SendAsync(methodName, @event, cancellationToken);
    }
}