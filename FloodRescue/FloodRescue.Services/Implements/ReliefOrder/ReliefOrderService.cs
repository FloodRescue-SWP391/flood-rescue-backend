using AutoMapper;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.DTO.Kafka;
using FloodRescue.Services.DTO.ReliefOrderRequest;
using FloodRescue.Services.DTO.Response.ReliefOrder;
using FloodRescue.Services.Interface.Kafka;
using FloodRescue.Services.Interface.ReliefOrder;
using FloodRescue.Services.Interface.RescueMission;
using FloodRescue.Services.DTO.Request.RescueMissionRequest;
using FloodRescue.Services.SharedSetting;

using ReliefOrderEntity = FloodRescue.Repositories.Entites.ReliefOrder;
using RescueRequestEntity = FloodRescue.Repositories.Entites.RescueRequest;
using RescueTeamEntity = FloodRescue.Repositories.Entites.RescueTeam;

using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FloodRescue.Services.Implements.ReliefOrder
{
    public class ReliefOrderService : IReliefOrder
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IKafkaProducerService _kafkaProducer;
        private readonly IRescueMissionService _rescueMissionService;

        private readonly ILogger<ReliefOrderService> _logger;

        public ReliefOrderService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IKafkaProducerService kafkaProducer,
            IRescueMissionService rescueMissionService,
            ILogger<ReliefOrderService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _kafkaProducer = kafkaProducer;
            _rescueMissionService = rescueMissionService;
            _logger = logger;
        }
   

        public async Task<ReliefOrderResponseDTO?> CreateReliefOrderAsync(ReliefOrderRequestDTO request)
        {
            RescueRequestEntity? rescueRequest = await _unitOfWork.RescueRequests.GetAsync(r => r.RescueRequestID == request.RescueRequestID && !r.IsDeleted);

            if (rescueRequest == null)
            {
                _logger.LogWarning("[ReliefOrderService - Sql Server] Cannot find Rescue Request with ID : {ID}", request.RescueRequestID);
                return null;
            }

            if(rescueRequest.RequestType != RescueRequestType.SUPPLY_TYPE)
            {
                _logger.LogWarning("[ReliefOrderService] Rescue Request must be supply type");
                return null;
            }

            RescueTeamEntity? rescueTeam = await _unitOfWork.RescueTeams.GetAsync(rt => request.RescueTeamID == rt.RescueTeamID && !rt.IsDeleted);

            if(rescueTeam == null)
            {
                _logger.LogWarning("[ReliefOrderService - Sql Server] Cannot find Rescue Team with Team ID: {ID}" , request.RescueTeamID);
                return null;
            }

            _logger.LogInformation("[ReliefOrderService] Start Transaction for Creating Relief Order");

            using var transaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                
                var order = new ReliefOrderEntity
                {
                    ReliefOrderID = Guid.NewGuid(),
                    RescueRequestID = request.RescueRequestID,
                    RescueTeamID = request.RescueTeamID,
                    Status = ReliefOrderSettings.PENDING_STATUS,
                    CreatedTime = DateTime.UtcNow,
                    Description = rescueRequest.Description,
                    IsDeleted = false
                };

                await _unitOfWork.ReliefOrders.AddAsync(order);


                var dispatchDto = new DispatchMissionRequestDTO
                {
                    RescueRequestID = request.RescueRequestID,
                    RescueTeamID = request.RescueTeamID
                };

                _logger.LogInformation("[ReliefOrderService] Start to call Dispatch Mission Async with Rescue Request ID: {ID} - Rescue Team ID {teamID}", request.RescueRequestID, request.RescueTeamID);

                var dispatchResult = await _rescueMissionService.DispatchMissionAsync(dispatchDto);

                if (dispatchResult == null)
                {
                    _logger.LogWarning("[ReliefOrderService] Call in dispatch result service but cannot found");
                    await transaction.RollbackAsync();
                    return null;
                }

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                // mapper ReliefOrder -> ReliefOrderMessage
                
                ReliefOrderMessage kafkaMessage = _mapper.Map<ReliefOrderMessage>(order);

                await _kafkaProducer.ProduceAsync(
                    topic: KafkaSettings.RELIEF_ORDER_CREATED_TOPIC,
                    key: order.ReliefOrderID.ToString(),
                    message: kafkaMessage);

                _logger.LogInformation("[ReliefOrderService - Kafka Consumer] Kafka message sent to topic {Topic}", KafkaSettings.RELIEF_ORDER_CREATED_TOPIC);

                return _mapper.Map<ReliefOrderResponseDTO>(order);
            }
            catch(Exception ex)
            {
                _logger.LogError("[ReliefOrderService - Error] Cannot Create Relief Order with Rescue Request ID: {ID} - Error: {error}. Transaction RollBack", request.RescueRequestID, ex.Message.ToString());
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}

