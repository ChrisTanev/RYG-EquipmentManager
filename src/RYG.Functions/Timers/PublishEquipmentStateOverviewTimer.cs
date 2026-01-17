namespace RYG.Functions.Timers;

public class PublishEquipmentStateOverviewTimer(
    IEquipmentService equipmentService,
    ILogger<PublishEquipmentStateOverviewTimer> logger)
{
    [Function("PublishEquipmentStateOverviewTimer")]
    public async Task Run(
        [TimerTrigger("*/10 * * * * *")] TimerInfo timerInfo, CancellationToken cancellationToken)
    {
        logger.LogInformation("PublishEquipmentStateOverviewTimer triggered at {Time}", DateTime.UtcNow);

        try
        {
            await equipmentService.PublishEquipmentStateOverviewAsync(cancellationToken);
            logger.LogInformation("PublishEquipmentStateOverviewTimer completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PublishEquipmentStateOverviewTimer failed");
            throw;
        }
    }
}