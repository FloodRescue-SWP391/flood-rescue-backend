using AutoMapper;
using FloodRescue.Repositories.Entites;
using FloodRescue.Services.DTO.Request.Auth;
using FloodRescue.Services.DTO.Request.User;
using FloodRescue.Services.DTO.Request.Warehouse;
using FloodRescue.Services.DTO.Response.AuthResponse;
using FloodRescue.Services.DTO.Response.RegisterResponse;
using FloodRescue.Services.DTO.Response.Warehouse;
using FloodRescue.Services.DTO.Request.Category;
using FloodRescue.Services.DTO.Response.Category;
using FloodRescue.Services.DTO.Request.ReliefItem;
using FloodRescue.Services.DTO.Response.ReliefItem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.Mapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Mapping UpdateUserRequestDTO -> User 
            CreateMap<CreateUserRequestDTO, User>();
            //Mapping chính xác các tên Property rồi nên ko cần ForMember nữa

            // Mapping User -> UserResponseDTO
            CreateMap<User, UserResponseDTO>()
                .ForMember(
                    destinationMember: destination => destination.RoleName,
                    opt => opt.MapFrom(src => src.Role != null ? src.Role.RoleName : string.Empty)
                    );

            //Mapping UpdateUserRequestDTO -> User
            CreateMap<UpdateUserRequestDTO, User>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Mapping User -> RegisterResponseDTO
            CreateMap<User, RegisterResponseDTO>();

            // Mapping RegisterRequestDTO -> User (ignore Password vì cần hash riêng)
            CreateMap<RegisterRequestDTO, User>()
                .ForMember(destination => destination.Password, opt => opt.Ignore());

            //Mapping CreateWarehouseRequestDTO -> WarehouseRequest
            CreateMap<CreateWarehouseRequestDTO, Warehouse>();

            //Mapping Warehouse -> CreateWarehouseResponseDTO
            CreateMap<Warehouse, CreateWarehouseResponseDTO>().ForMember(
                    destinationMember: destination => destination.CreatedBy,
                    opt => opt.MapFrom(src => src.Manager != null ? src.Manager.FullName : string.Empty)
                );

            // Mapping Warehouse -> ShowWareHouseResponseDTO
            CreateMap<Warehouse, ShowWareHouseResponseDTO>().ForMember(
                destinationMember: destination => destination.ManagedBy,
                opt => opt.MapFrom(src => src.Manager != null ? src.Manager.FullName : string.Empty)
            );

            // Mapping Warehouse -> UpdateWarehouseResponseDTO
            CreateMap<Warehouse, UpdateWarehouseResponseDTO>();

            // Mapping Category DTOs
            CreateMap<CreateCategoryRequestDTO, Category>();

            CreateMap<Category, CategoryResponseDTO>();

            // Mapping ReliefItem DTOs
            CreateMap<CreateReliefItemRequestDTO, ReliefItem>();

            CreateMap<ReliefItem, ReliefItemResponseDTO>();
        }
    }
}
