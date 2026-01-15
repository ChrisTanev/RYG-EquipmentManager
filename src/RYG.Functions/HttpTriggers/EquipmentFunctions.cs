using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;

namespace RYG.Functions.HttpTriggers;

public class EquipmentFunctions(
    IEquipmentService equipmentService,
    IValidator<CreateEquipmentRequest> createValidator,
    IValidator<ChangeStateRequest> stateValidator,
    ILogger<EquipmentFunctions> logger)
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