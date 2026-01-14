using Microsoft.Extensions.DependencyInjection;
using RYG.Infrastructure.Persistence;

namespace RYG.Infrastructure.Extensions;

public static class ServiceProviderExtensions
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();
    }
}