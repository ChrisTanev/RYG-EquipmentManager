using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

namespace RYG.Functions.HttpTriggers;

public class CreateEquipmentFunction(
    IEquipmentService equipmentService,
    IValidator<CreateEquipmentRequest> createValidator,
    ILogger<CreateEquipmentFunction> logger)
{
    [Function("CreateEquipment")]
    [OpenApiOperation("CreateEquipment", "Equipment", Summary = "Create new equipment",
        Description = "Creates a new equipment item with the specified name and initial state")]
    [OpenApiRequestBody("application/json", typeof(CreateEquipmentRequest), Required = true,
        Description = "Equipment creation request")]
    [OpenApiResponseWithBody(HttpStatusCode.Created, "application/json", typeof(EquipmentDto),
        Description = "Equipment created")]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Invalid request")]
    public async Task<IActionResult> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "equipment")]
        HttpRequest req,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = await req.ReadFromJsonAsync<CreateEquipmentRequest>(cancellationToken) ??
                          throw new JsonException();

            var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                return new BadRequestObjectResult(validationResult.Errors.Select(e => e.ErrorMessage));

            logger.LogInformation("Creating equipment with name {EquipmentName}", request.Name);
            var equipment = await equipmentService.CreateAsync(request, cancellationToken);

            return new CreatedResult($"/api/equipment/{equipment.Id}", equipment);
        }
        catch (Exception e)
        {
            return new BadRequestObjectResult(e.Message);
        }
    }
}