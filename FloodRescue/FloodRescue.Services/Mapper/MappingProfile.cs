using AutoMapper;
using FloodRescue.Repositories.Entites;
using FloodRescue.Services.DTO.Kafka;
using FloodRescue.Services.DTO.Request.Auth;
using FloodRescue.Services.DTO.Request.Category;
using FloodRescue.Services.DTO.Request.ReliefItem;
using FloodRescue.Services.DTO.Request.RescueRequest;
using FloodRescue.Services.DTO.Request.RescueTeamRequest;
using FloodRescue.Services.DTO.Request.User;
using FloodRescue.Services.DTO.Request.Warehouse;
using FloodRescue.Services.DTO.Request.WarehouseRequest;
using FloodRescue.Services.DTO.Response.AuthResponse;
using FloodRescue.Services.DTO.Response.Category;
using FloodRescue.Services.DTO.Response.IncidentResponse;
using FloodRescue.Services.DTO.Response.RegisterResponse;
using FloodRescue.Services.DTO.Response.ReliefItem;
using FloodRescue.Services.DTO.Response.ReliefOrder;
using FloodRescue.Services.DTO.Response.RescueMissionResponse;
using FloodRescue.Services.DTO.Response.RescueRequestResponse;
using FloodRescue.Services.DTO.Response.RescueTeamResponse;
using FloodRescue.Services.DTO.Response.Warehouse;
using FloodRescue.Services.DTO.SignalR;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net;
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

            CreateMap<Warehouse, CreateWarehouseResponseDTO>();

            // Mapping Warehouse -> UpdateWarehouseResponseDTO
            CreateMap<Warehouse, UpdateWarehouseResponseDTO>();
            CreateMap<UpdateWarehouseRequestDTO, Warehouse>();

            CreateMap<Warehouse, ShowWareHouseResponseDTO>();

            // Mapping Category DTOs
            CreateMap<CreateCategoryRequestDTO, Category>();

            CreateMap<Category, CategoryResponseDTO>();

            // Mapping ReliefItem DTOs
            CreateMap<CreateReliefItemRequestDTO, ReliefItem>();

            CreateMap<ReliefItem, ReliefItemResponseDTO>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.CategoryName : string.Empty))
                .ForMember(dest => dest.UnitName, opt => opt.MapFrom(src => src.Unit != null ? src.Unit.UnitName : string.Empty));

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
                .ForMember(destinationMember: dest => dest.MissionStatus, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.CoordinatorID, opt => opt.MapFrom(src => src.CoordinatorID));

            CreateMap<RescueRequest, TeamAcceptedMessage>()
                .ForMember(destinationMember: dest => dest.RequestShortCode, opt => opt.MapFrom(src => src.ShortCode));

            CreateMap<RescueTeam, TeamAcceptedMessage>();

            //mapper rescue mission -> team reject message  
            //mapper rescue request -> team reject message  
            //mapper rescue team -> team reject message

            CreateMap<RescueMission, TeamRejectedMessage>()
                .ForMember(destinationMember: dest => dest.MissionStatus, opt => opt.MapFrom(src => src.Status))
                .ForMember(destinationMember: dest => dest.CoordinatorID, opt => opt.MapFrom(src => src.CoordinatorID));
    
            CreateMap<RescueRequest, TeamRejectedMessage>().ForMember(destinationMember: dest => dest.RequestShortCode, opt => opt.MapFrom(src => src.ShortCode));

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
                .ForMember(dest => dest.CitizenEmail, opt => opt.MapFrom(src => src.CitizenEmail))
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

            // mapper ReliefOrder -> ReliefOrderMessage
            // mapper Rescue Request -> ReliefOrderMessage chủ yếu để xài description và RescueRequestID
            CreateMap<ReliefOrder, ReliefOrderMessage>(); 

            //Soạn notification phải có description rồi mới gửi cho manager, còn rescue team thì không cần
            //Trong auto mapper không cần description
            // mapper ReliefOrderMessage -> ReliefOrderNotification
            CreateMap<ReliefOrderMessage, ReliefOrderNotification>().ForMember(dest => dest.Message, opt => opt.Ignore());

            // mapper MissionCompletedMessage -> MissionCompletedNotification
            CreateMap<MissionCompletedMessage, MissionCompletedNotification>().ForMember(dest => dest.Message, opt => opt.Ignore());



            // mapper OrderPreparedMessage -> OrderPreparedNotification
            CreateMap<OrderPreparedMessage, OrderPreparedNotification>().ForMember(dest => dest.Message, opt => opt.Ignore())
                                                 .ForMember(dest => dest.OrderMessages, opt => opt.MapFrom(src => src.Items));

            // mapper DeliveryStartedMessage -> DeliveryStartedNotification
            CreateMap<DeliveryStartedMessage, DeliveryStartedNotification>().ForMember(dest => dest.Message, opt => opt.Ignore());

            // mapper IncidentResolvedMessage -> IncidentResolvedNotification
            CreateMap<IncidentResolvedMessage, IncidentResolvedNotification>().ForMember(dest => dest.Message, opt => opt.Ignore());

            // mapper IncidentReportedMessage -> IncidentReportedNotification
            CreateMap<IncidentReportedMessage, IncidentReportedNotification>().ForMember(dest => dest.Message, opt => opt.Ignore());


            CreateMap<RescueRequest, RescueRequestListResponseDTO>()
                .ForMember(dest => dest.RequestType, opt => opt.MapFrom(src => src.RequestType));

            CreateMap<RescueMission, RescueMissionListResponseDTO>()
                .ForMember(dest => dest.CitizenAddress, opt => opt.MapFrom(src => src.RescueRequest != null ? src.RescueRequest.Address : null))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status));

            // Mapping IncidentReport -> IncidentDetailResponseDTO
            CreateMap<IncidentReport, IncidentDetailResponseDTO>()
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Latitiude)) // Fix typo từ entity
                .ForMember(dest => dest.ReporterName, opt => opt.MapFrom(src => src.Reported != null ? src.Reported.FullName : "Unknown"))
                .ForMember(dest => dest.ResolverName, opt => opt.MapFrom(src => src.Resolver != null ? src.Resolver.FullName : null))
                .ForMember(dest => dest.TeamName, opt => opt.Ignore()); // Sẽ set thủ công vì cần query thêm


            // Mapping RescueMission -> RescueMissionDetailResponseDTO
            CreateMap<RescueMission, RescueMissionDetailResponseDTO>()
                .ForMember(dest => dest.TeamName, opt => opt.MapFrom(src => src.RescueTeam != null ? src.RescueTeam.TeamName : "Unknown"))
                .ForMember(dest => dest.RequestInfo, opt => opt.Ignore()); // Sẽ map thủ công từ RescueRequest

            // Mapping RescueRequest -> VictimInfoDTO
            CreateMap<RescueRequest, VictimInfoDTO>();

            // Mapping RescueTeamMember -> RescueTeamMemberResponseDTO
            CreateMap<RescueTeamMember, RescueTeamMemberResponseDTO>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : string.Empty))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.User != null ? src.User.Phone : string.Empty));
        }
    }
}
