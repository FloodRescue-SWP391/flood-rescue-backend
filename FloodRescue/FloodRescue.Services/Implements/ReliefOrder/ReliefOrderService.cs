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

namespace FloodRescue.Services.Implements.ReliefOrder
{
    public class ReliefOrderService : IReliefOrder
    {
        private const string RequestTypeSupplies = "Supplies";
        private const string ReliefOrderStatusPending = "Pending";

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IKafkaProducerService _kafkaProducer;
        private readonly IRescueMissionService _rescueMissionService;

        public ReliefOrderService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IKafkaProducerService kafkaProducer,
            IRescueMissionService rescueMissionService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _kafkaProducer = kafkaProducer;
            _rescueMissionService = rescueMissionService;
        }
   

        public async Task<ReliefOrderResponseDTO> CreateAsync(ReliefOrderRequestDTO request)
        {
            var rescueRequest = await _unitOfWork.RescueRequests.GetAsync(r => r.RescueRequestID == request.RescueRequestId && !r.IsDeleted);
            if (rescueRequest == null)
            {
                throw new InvalidOperationException("Rescue request not found");
            }

            if (!string.Equals(rescueRequest.RequestType, RequestTypeSupplies, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Rescue request is not supplies type");
            }

            await using var tx = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var order = new ReliefOrderEntity
                {
                    ReliefOrderID = Guid.NewGuid(),
                    RescueRequestID = request.RescueRequestId,
                    RescueTeamID = request.RescueTeamId,
                    Status = ReliefOrderStatusPending,
                    CreatedTime = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _unitOfWork.ReliefOrders.AddAsync(order);

                var dispatchDto = new DispatchMissionRequestDTO
                {
                    RescueRequestID = request.RescueRequestId,
                    RescueTeamID = request.RescueTeamId
                };

                var dispatchResult = await _rescueMissionService.DispatchMissionAsync(dispatchDto);
                if (dispatchResult == null)
                {
                    await tx.RollbackAsync();
                    throw new InvalidOperationException("DispatchMissionAsync failed");
                }

                await _unitOfWork.SaveChangesAsync();
                await tx.CommitAsync();

                var kafkaMessage = new ReliefOrderMessage
                {
                    ReliefOrderId = order.ReliefOrderID,
                    RescueRequestId = order.RescueRequestID,
                    RescueTeamId = order.RescueTeamID ?? Guid.Empty,
                    Status = order.Status,
                    CreatedTime = order.CreatedTime
                };

                await _kafkaProducer.ProduceAsync(
                    topic: KafkaSettings.RELIEF_ORDER_CREATED_TOPIC,
                    key: order.ReliefOrderID.ToString(),
                    message: kafkaMessage);

                return _mapper.Map<ReliefOrderResponseDTO>(order);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
