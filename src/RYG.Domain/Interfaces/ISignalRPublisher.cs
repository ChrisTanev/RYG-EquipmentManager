namespace RYG.Domain.Interfaces;

public interface ISignalRPublisher
{
    Task SendToClientAsync<TEvent>(TEvent @event, string methodName, CancellationToken cancellationToken = default)
        where TEvent : class;
}