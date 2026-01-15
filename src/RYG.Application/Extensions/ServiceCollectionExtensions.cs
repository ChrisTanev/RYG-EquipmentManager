using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RYG.Application.Mappings;
using RYG.Application.Services;

namespace RYG.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(MappingProfile).Assembly); // TODO Add all profiles in the assembly

        services.AddValidatorsFromAssemblyContaining<MappingProfile>(); // TODO register all

        services.AddScoped<IEquipmentService, EquipmentService>();
        services.AddSingleton<IOrderService, OrderService>();

        return services;
    }
}