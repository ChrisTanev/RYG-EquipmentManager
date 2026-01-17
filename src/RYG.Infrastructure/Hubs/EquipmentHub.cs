using Microsoft.AspNetCore.SignalR;

namespace RYG.Infrastructure.Hubs;

public class EquipmentHub : Hub
{
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }
}