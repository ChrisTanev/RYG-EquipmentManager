using AutoMapper;
using RYG.Application.DTOs;

namespace RYG.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Equipment, EquipmentDto>();
        CreateMap<Order, OrderDto>();
    }
}