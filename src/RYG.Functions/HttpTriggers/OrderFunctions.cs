using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

namespace RYG.Functions.HttpTriggers;

public class OrderFunctions(
    IOrderService orderService,
    IValidator<CreateOrderRequest> createValidator,
    ILogger<OrderFunctions> logger)
{
    [Function("CreateOrder")]
    [OpenApiOperation("CreateOrder", "Orders", Summary = "Create new order",
        Description = "Creates a new order scheduled to an equipment")]
    [OpenApiRequestBody("application/json", typeof(CreateOrderRequest), Required = true,
        Description = "Order creation request")]
    [OpenApiResponseWithBody(HttpStatusCode.Created, "application/json", typeof(OrderDto),
        Description = "Order created")]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Invalid request")]
    public async Task<IActionResult> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders")]
        HttpRequest req,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = await req.ReadFromJsonAsync<CreateOrderRequest>(cancellationToken) ??
                          throw new JsonException();

            var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                return new BadRequestObjectResult(validationResult.Errors.Select(e => e.ErrorMessage));

            logger.LogInformation("Creating order for equipment {EquipmentId}", request.EquipmentId);
            await orderService.CreateAsync(request, cancellationToken);

            return new OkObjectResult("Order created");
        }
        catch (Exception e)
        {
            return new BadRequestObjectResult(e.Message);
        }
    }
}