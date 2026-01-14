namespace RYG.Functions.HttpTriggers;

public class SignalRFunctions(ILogger<SignalRFunctions> logger)
{
    [Function("negotiate")]
    public SignalRConnectionInfo Negotiate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "negotiate")]
        HttpRequest req,
        [SignalRConnectionInfoInput(HubName = "equipment", ConnectionStringSetting = "AzureSignalRConnectionString")]
        SignalRConnectionInfo connectionInfo)
    {
        logger.LogInformation("SignalR negotiate requested");
        return connectionInfo;
    }
}