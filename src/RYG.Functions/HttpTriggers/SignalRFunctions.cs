namespace RYG.Functions.HttpTriggers;

public class SignalRFunctions(ILogger<SignalRFunctions> logger)
{
    // TODO Needed?
    // TODO make sure one lines are still wrapped in {}
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