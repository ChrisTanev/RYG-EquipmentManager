using System.Net;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;

namespace RYG.Functions.HttpTriggers;

public class GetEquipmentByIdFunction(IEquipmentService equipmentService, ILogger<GetEquipmentByIdFunction> logger)
{
    [Function("GetEquipmentById")]
    [OpenApiOperation("GetEquipmentById", "Equipment", Summary = "Get equipment by ID",
        Description = "Returns a single equipment item by its ID")]
    [OpenApiParameter("id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid),
        Description = "Equipment ID")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(EquipmentDto),
        Description = "Equipment found")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "Equipment not found")]
    public async Task<IActionResult> GetById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "equipment/{id:guid}")]
        HttpRequest req,
        Guid id,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting equipment {EquipmentId}", id);
        try
        {
            var equipment = await equipmentService.GetByIdAsync(id, cancellationToken);

            return new OkObjectResult(equipment);
        }
        catch (Exception e)
        {
            return new BadRequestObjectResult(e.Message);
        }
    }
}