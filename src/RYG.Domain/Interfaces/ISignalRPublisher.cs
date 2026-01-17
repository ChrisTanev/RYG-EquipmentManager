namespace RYG.Domain.Interfaces;

public interface ISignalRPublisher
{
    Task SendToClientAsync<TEvent>(TEvent @event, string methodName, CancellationToken cancellationToken = default)
        where TEvent : class;

    Task SendToGroupAsync<TEvent>(TEvent @event, string methodName, string groupName,
        CancellationToken cancellationToken = default)
        where TEvent : class;
}