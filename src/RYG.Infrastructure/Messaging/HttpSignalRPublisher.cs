using System.Net.Http.Json;

namespace RYG.Infrastructure.Messaging;

/// <summary>
/// SignalR publisher that communicates with a self-hosted SignalR Hub via HTTP
/// </summary>
public class HttpSignalRPublisher(HttpClient httpClient, ILogger<HttpSignalRPublisher> logger) : ISignalRPublisher
{
    public async Task SendToClientAsync<TEvent>(TEvent @event, string methodName,
        CancellationToken cancellationToken = default)
        where TEvent : class
    {
        try
        {
            var request = new SignalRBroadcastRequest
            {
                MethodName = methodName,
                Data = @event
            };

            var response = await httpClient.PostAsJsonAsync("/api/broadcast", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            logger.LogInformation("Sent SignalR message: {MethodName}", methodName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send SignalR message: {MethodName}", methodName);
            throw;
        }
    }

    public async Task SendToGroupAsync<TEvent>(TEvent @event, string methodName, string groupName,
        CancellationToken cancellationToken = default)
        where TEvent : class
    {
        try
        {
            var request = new SignalRBroadcastRequest
            {
                MethodName = methodName,
                Data = @event,
                GroupName = groupName
            };

            var response = await httpClient.PostAsJsonAsync("/api/broadcast", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            logger.LogInformation("Sent SignalR message: {MethodName} to group: {GroupName}", methodName, groupName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send SignalR message: {MethodName} to group: {GroupName}", methodName,
                groupName);
            throw;
        }
    }

    private class SignalRBroadcastRequest
    {
        public string MethodName { get; set; } = string.Empty;
        public object? Data { get; set; }
        public string? GroupName { get; set; }
    }
}
