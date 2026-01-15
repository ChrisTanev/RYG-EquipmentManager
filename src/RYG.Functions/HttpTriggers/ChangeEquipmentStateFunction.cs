using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;

namespace RYG.Functions.HttpTriggers;

public class ChangeEquipmentStateFunction(
    IEquipmentService equipmentService,
    IValidator<ChangeStateRequest> stateValidator,
    ILogger<ChangeEquipmentStateFunction> logger)
{
    [Function("ChangeEquipmentState")]
    [OpenApiOperation("ChangeEquipmentState", "Equipment", Summary = "Change equipment state",
        Description = "Changes the state of an equipment item (Red/Yellow/Green)")]
    [OpenApiParameter("id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid),
        Description = "Equipment ID")]
    [OpenApiRequestBody("application/json", typeof(ChangeStateRequest), Required = true,
        Description = "State change request")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(EquipmentDto),
        Description = "State changed")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "Equipment not found")]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Invalid request")]
    public async Task<IActionResult> ChangeState(
        [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "equipment/{id:guid}/state")]
        HttpRequest req,
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = await req.ReadFromJsonAsync<ChangeStateRequest>(cancellationToken) ??
                          throw new JsonException();

            var validationResult = await stateValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                return new BadRequestObjectResult(validationResult.Errors.Select(e => e.ErrorMessage));

            logger.LogInformation("Changing state of equipment {EquipmentId} to {State}", id, request.State);
            var equipment = await equipmentService.ChangeStateAsync(id, request, cancellationToken);

            return new OkObjectResult(equipment);
        }
        catch (Exception e)
        {
            return new BadRequestObjectResult(e.Message);
        }
    }
}