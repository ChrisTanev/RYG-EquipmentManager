namespace RYG.Functions.Timers;

public class ProcessOrdersTimer(
    IOrderService orderService,
    ILogger<ProcessOrdersTimer> logger)
{
    [Function("ProcessOrdersTimer")]
    public async Task Run(
        [TimerTrigger("0 */1 * * * *")] TimerInfo timerInfo,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("ProcessOrdersTimer triggered at {Time}", DateTime.UtcNow);

        try
        {
            await orderService.ProcessQueuedOrdersAsync(cancellationToken);
            logger.LogInformation("ProcessOrdersTimer completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ProcessOrdersTimer failed");
            throw;
        }
    }
}