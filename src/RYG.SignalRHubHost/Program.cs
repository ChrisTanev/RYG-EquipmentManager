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
        builder.Services.AddControllers();
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        var app = builder.Build();

        app.UseCors();
        app.MapHub<EquipmentHub>("/equipmentHub");
        app.MapControllers();

        await app.RunAsync();
    }
}