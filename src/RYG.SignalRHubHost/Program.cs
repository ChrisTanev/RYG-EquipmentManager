using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using RYG.Infrastructure.Hubs;

namespace RYG.SignalRHubHost;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSignalR();

        var app = builder.Build();

        app.MapHub<EquipmentHub>("/equipmentHub");
        await app.RunAsync();
    }
}