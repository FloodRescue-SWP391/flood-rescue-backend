using AutoMapper;
using FloodRescue.Repositories.Entites;
using FloodRescue.Services.DTO.Request.Auth;
using FloodRescue.Services.DTO.Request.Category;
using FloodRescue.Services.DTO.Request.ReliefItem;
using FloodRescue.Services.DTO.Request.RescueRequest;
using FloodRescue.Services.DTO.Request.RescueTeamRequest;
using FloodRescue.Services.DTO.Request.User;
using FloodRescue.Services.DTO.Request.Warehouse;
using FloodRescue.Services.DTO.Response.AuthResponse;
using FloodRescue.Services.DTO.Response.Category;
using FloodRescue.Services.DTO.Response.RegisterResponse;
using FloodRescue.Services.DTO.Response.ReliefItem;
using FloodRescue.Services.DTO.Response.RescueRequestResponse;
using FloodRescue.Services.DTO.Response.RescueTeamResponse;
using FloodRescue.Services.DTO.Response.Warehouse;
using FloodRescue.Services.DTO.Response.ReliefOrder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FloodRescue.Services.DTO.Kafka;
using FloodRescue.Services.DTO.SignalR;
using FloodRescue.Services.DTO.Response.RescueMissionResponse;
using System.Net;

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
            CreateMap<User, RegisterResponseDTO>()
                .ForMember(
                    destinationMember: destination => destination.RoleName,
                    opt => opt.MapFrom(src => src.Role != null ? src.Role.RoleName : string.Empty)
                    );

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

            // Mapping ReliefOrder DTOs
            CreateMap<ReliefOrder, ReliefOrderResponseDTO>();
            // Mapping UpdateUserRequestDTO -> Warehouse
            CreateMap<UpdateUserRequestDTO, Warehouse>();

            CreateMap<MissionAssignedMessage, MissionAssignedNotification>()
                .ForMember(
                    destinationMember: destination => destination.Title,
                    opt => opt.MapFrom(src => "New Rescue Mission Assigned"))
                .ForMember(
                    destinationMember: destination => destination.NotificationType,
                    opt => opt.MapFrom(src => "MissionAssigned")
                )
                .ForMember(
                    destinationMember: destination => destination.ActionMessage,
                    opt => opt.MapFrom(src => "Please proceed to the rescue location immediately in 5 minutes.")
                );
            
            
            CreateMap<RescueRequest, MissionAssignedMessage>()
                .ForMember(dest => dest.RequestShortCode, opt => opt.MapFrom(src => src.ShortCode));


            CreateMap<RescueTeam, MissionAssignedMessage>();
  
            CreateMap<RescueMission, MissionAssignedMessage>()
                .ForMember(dest => dest.MissionID, opt => opt.MapFrom(src => src.RescueMissionID))
                .ForMember(dest => dest.MissionStatus, opt => opt.MapFrom(src => src.Status));


            CreateMap<RescueMission, DispatchMissionResponseDTO>()
                .ForMember(dest => dest.MissionStatus, opt => opt.MapFrom(src => src.Status));

            CreateMap<RescueRequest, DispatchMissionResponseDTO>()
                .ForMember(dest => dest.RequestShortCode, opt => opt.MapFrom(src => src.ShortCode));

            CreateMap<RescueTeam, DispatchMissionResponseDTO>();

            // mappper rescue mission -> team accept message
            // mapper rescue request -> team accept message 
            // mapper rescue team -> team accept message    

            CreateMap<RescueMission, TeamAcceptedMessage>()
                .ForMember(destinationMember: dest => dest.MissionStatus, opt => opt.MapFrom(src => src.Status));

            CreateMap<RescueRequest, TeamAcceptedMessage>()
                .ForMember(destinationMember: dest => dest.RequestShortCode, opt => opt.MapFrom(src => src.ShortCode))
                .ForMember(destinationMember: dest => dest.CoordinatorID, opt => opt.MapFrom(src => src.CoordinatorID));

            CreateMap<RescueTeam, TeamAcceptedMessage>();

            //mapper rescue mission -> team reject message  
            //mapper rescue request -> team reject message  
            //mapper rescue team -> team reject message

            CreateMap<RescueMission, TeamRejectedMessage>()
                .ForMember(destinationMember: dest => dest.MissionStatus, opt => opt.MapFrom(src => src.Status));

            CreateMap<RescueRequest, TeamRejectedMessage>().ForMember(destinationMember: dest => dest.RequestShortCode, opt => opt.MapFrom(src => src.ShortCode))
                                                            .ForMember(destinationMember: dest => dest.CoordinatorID, opt => opt.MapFrom(src => src.CoordinatorID));

            CreateMap<RescueTeam, TeamRejectedMessage>();

            //mapper rescue misson -> respond mission response dto
            //mapper rescue request -> respond mission response dto 
            //mapper rescue team -> respond mission response dto    
            //mappper respond mission requesst dto -> respond mission response dto  - map tay

            CreateMap<RescueMission, RespondMissionResponseDTO>().ForMember(
                    destinationMember: dest => dest.NewMissionStatus, opt => opt.MapFrom(src => src.Status)
            );

            CreateMap<RescueRequest, RespondMissionResponseDTO>().ForMember(
                destinationMember: dest => dest.RequestShortCode, opt => opt.MapFrom(src => src.ShortCode));

            CreateMap<RescueTeam, RespondMissionResponseDTO>();


            //mapper TeamAcceptedMessage -> TeamAcceptedNotification
            CreateMap<TeamAcceptedMessage, TeamAcceptedNotification>();

            //mapper TeamRejectedMessage -> TeamRejectedNotification
            CreateMap<TeamRejectedMessage, TeamRejectedNotification>();

            // Mapping RescueTeamRequestDTO -> RescueTeam
            CreateMap<RescueTeamRequestDTO, RescueTeam>();

            // Mapping RescueTeam -> RescueTeamResponseDTO
            CreateMap<RescueTeam, RescueTeamResponseDTO>();

            // CreateRescueRequestDTO -> RescueRequest
            // PhoneNumber (DTO) != CitizenPhone (Entity) → ForMember
            // ShortCode, Status, CreatedTime → service tự set nên Ignore
            CreateMap<CreateRescueRequestDTO, RescueRequest>()
                .ForMember(dest => dest.CitizenPhone, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.ShortCode, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedTime, opt => opt.Ignore())
                .ForMember(dest => dest.RescueRequestID, opt => opt.Ignore());

            // RescueRequest -> CreateRescueRequestResponseDTO
            // ImageUrls không có trong entity → service sẽ set thủ công
            CreateMap<RescueRequest, CreateRescueRequestResponseDTO>()
                .ForMember(dest => dest.ImageUrls, opt => opt.Ignore());

            // Mapping RescueRequest -> RescueRequestKafkaMessage
            CreateMap<RescueRequest, RescueRequestKafkaMessage>(); 
        }
    }
}
