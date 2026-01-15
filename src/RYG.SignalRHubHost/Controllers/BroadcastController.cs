using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RYG.Infrastructure.Hubs;

namespace RYG.SignalRHubHost.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BroadcastController(IHubContext<EquipmentHub> hubContext, ILogger<BroadcastController> logger)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] BroadcastRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MethodName))
            return BadRequest("MethodName is required");

        logger.LogInformation("Broadcasting SignalR message: {MethodName}", request.MethodName);

        await hubContext.Clients.All.SendAsync(request.MethodName, request.Data);

        return Ok();
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", service = "SignalR Hub" });
    }
}

public record BroadcastRequest
{
    public string MethodName { get; init; } = string.Empty;
    public object? Data { get; init; }
}
