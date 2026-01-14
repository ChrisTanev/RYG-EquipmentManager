using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RYG.Application.Mappings;
using RYG.Application.Services;

namespace RYG.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(MappingProfile).Assembly);

        services.AddValidatorsFromAssemblyContaining<MappingProfile>();

        services.AddScoped<IEquipmentService, EquipmentService>();

        return services;
    }
}