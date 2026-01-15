using System.Net;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

namespace RYG.Functions.HttpTriggers;

public class GetAllEquipmentFunction(IEquipmentService equipmentService, ILogger<GetAllEquipmentFunction> logger)
{
    [Function("GetAllEquipment")]
    [OpenApiOperation("GetAllEquipment", "Equipment", Summary = "Get all equipment",
        Description = "Returns a list of all equipment with their current states")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(IEnumerable<EquipmentDto>),
        Description = "List of equipment")]
    public async Task<IActionResult> GetAll(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "equipment")]
        HttpRequest req,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting all equipment");
        var equipment = await equipmentService.GetAllAsync(cancellationToken);
        return new OkObjectResult(equipment);
    }
}