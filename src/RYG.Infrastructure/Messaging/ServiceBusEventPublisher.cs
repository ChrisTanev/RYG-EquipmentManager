using System.Text.Json;
using Azure.Messaging.ServiceBus;

namespace RYG.Infrastructure.Messaging;

public class ServiceBusEventPublisher(
    ServiceBusClient client,
    string topicName,
    ILogger<ServiceBusEventPublisher> logger) : IEventPublisher, IAsyncDisposable
{
    private readonly ServiceBusSender _sender = client.CreateSender(topicName);

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await client.DisposeAsync();
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        var eventType = typeof(TEvent).Name;
        var messageBody = JsonSerializer.Serialize(@event);

        var message = new ServiceBusMessage(messageBody)
        {
            ContentType = "application/json",
            Subject = eventType
        };

        await _sender.SendMessageAsync(message, cancellationToken);

        logger.LogInformation("Published {EventType} event to Service Bus", eventType);
    }
}