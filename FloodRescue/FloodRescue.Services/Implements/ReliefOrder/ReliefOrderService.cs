using AutoMapper;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.DTO.Kafka;
using FloodRescue.Services.DTO.Response.ReliefOrder;
using FloodRescue.Services.DTO.Response.ReliefOrderResponse;
using FloodRescue.Services.Interface.Kafka;
using FloodRescue.Services.Interface.ReliefOrder;
using FloodRescue.Services.Interface.RescueMission;
using FloodRescue.Services.DTO.Request.RescueMissionRequest;
using FloodRescue.Services.SharedSetting;
using FloodRescue.Services.Interface.Cache;

using ReliefOrderEntity = FloodRescue.Repositories.Entites.ReliefOrder;
using RescueRequestEntity = FloodRescue.Repositories.Entites.RescueRequest;
using RescueTeamEntity = FloodRescue.Repositories.Entites.RescueTeam;
using RescueMissionEntity = FloodRescue.Repositories.Entites.RescueMission;
using InventoryEntity = FloodRescue.Repositories.Entites.Inventory;
using ReliefOrderDetailEntity = FloodRescue.Repositories.Entites.ReliefOrderDetail;

using Microsoft.Extensions.Logging;
using System.Text.Json;
using FloodRescue.Services.DTO.Request.ReliefOrderRequest;

namespace FloodRescue.Services.Implements.ReliefOrder
{
    public class ReliefOrderService : IReliefOrder
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IKafkaProducerService _kafkaProducer;
        private readonly IRescueMissionService _rescueMissionService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<ReliefOrderService> _logger;

        private const string PENDING_ORDERS_CACHE_KEY = "relieforder:pending";

        public ReliefOrderService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IKafkaProducerService kafkaProducer,
            IRescueMissionService rescueMissionService,
            ICacheService cacheService,
            ILogger<ReliefOrderService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _kafkaProducer = kafkaProducer;
            _rescueMissionService = rescueMissionService;
            _cacheService = cacheService;
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

            await _unitOfWork.BeginTransactionAsync();

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
                    await _unitOfWork.RollbackTransactionAsync();
                    return null;
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

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
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<List<PendingOrderResponseDTO>> GetPendingOrdersAsync()
        {
            _logger.LogInformation("[ReliefOrderService] Starting GetPendingOrders");

            // Kiểm tra cache trước
            var cached = await _cacheService.GetAsync<List<PendingOrderResponseDTO>>(PENDING_ORDERS_CACHE_KEY);
            if (cached != null)
            {
                _logger.LogInformation("[ReliefOrderService - Redis] Cache hit for pending orders. Returned {Count} item(s)", cached.Count);
                return cached;
            }

            _logger.LogInformation("[ReliefOrderService - Redis] Cache miss for pending orders. Querying database");

            // Query ReliefOrders với Status == Pending, include RescueTeam
            List<ReliefOrderEntity> pendingOrders = await _unitOfWork.ReliefOrders.GetAllAsync(
                (ReliefOrderEntity ro) => ro.Status == ReliefOrderSettings.PENDING_STATUS && !ro.IsDeleted,
                ro => ro.RescueTeam!);

            _logger.LogInformation("[ReliefOrderService - Sql Server] Found {Count} pending relief order(s)", pendingOrders.Count);

            if (pendingOrders.Count == 0)
            {
                return new List<PendingOrderResponseDTO>();
            }

            // Lấy danh sách RescueRequestID từ các pending orders
            List<Guid> rescueRequestIds = pendingOrders.Select(ro => ro.RescueRequestID).Distinct().ToList();

            // Query RescueMissions theo danh sách RescueRequestID để lấy MissionID và MissionStatus
            List<RescueMissionEntity> missions = await _unitOfWork.RescueMissions.GetAllAsync(
                (RescueMissionEntity rm) => rescueRequestIds.Contains(rm.RescueRequestID) && !rm.IsDeleted);

            _logger.LogInformation("[ReliefOrderService - Sql Server] Found {Count} rescue mission(s) related to pending orders", missions.Count);

            // Map dữ liệu vào list PendingOrderResponseDTO
            List<PendingOrderResponseDTO> result = pendingOrders.Select(order =>
            {
                // Tìm mission tương ứng với RescueRequestID của order
                RescueMissionEntity? mission = missions.FirstOrDefault(m => m.RescueRequestID == order.RescueRequestID);

                return new PendingOrderResponseDTO
                {
                    ReliefOrderID = order.ReliefOrderID,
                    RescueRequestID = order.RescueRequestID,
                    CreatedTime = order.CreatedTime,
                    OrderStatus = order.Status,
                    MissionID = mission?.RescueMissionID,
                    MissionStatus = mission?.Status,
                    TeamName = order.RescueTeam?.TeamName
                };
            }).ToList();

            _logger.LogInformation("[ReliefOrderService] Successfully mapped {Count} pending order(s) to response", result.Count);

            await _cacheService.SetAsync(PENDING_ORDERS_CACHE_KEY, result, TimeSpan.FromMinutes(5));
            _logger.LogInformation("[ReliefOrderService - Redis] Cached {Count} pending order(s)", result.Count);

            return result;
        }
        
        public async Task<ReliefOrderResponseDTO?> PrepareReliefOrderAsync(PrepareOrderRequestDTO request, Guid managerID)
        {
            _logger.LogInformation("[ReliefOrderService] Start PrepareReliefOrderAsync for ReliefOrderID: {ID}", request.ReliefOrderID);

            // Validate ReliefOrder
            ReliefOrderEntity? reliefOrder = await _unitOfWork.ReliefOrders.GetAsync(o => o.ReliefOrderID == request.ReliefOrderID && !o.IsDeleted);

            if (reliefOrder == null)
            {
                _logger.LogWarning("[ReliefOrderService - Sql Server] Cannot find Relief Order with ID: {ID}", request.ReliefOrderID);
                return null;
            }

            if (reliefOrder.Status != ReliefOrderSettings.PENDING_STATUS)
            {
                _logger.LogWarning("[ReliefOrderService] Relief Order status must be Pending. Current status: {Status}", reliefOrder.Status);
                return null;
            }

            // Validate RescueMission related to this order (same RescueRequestID and RescueTeamID)
            RescueMissionEntity? rescueMission = await _unitOfWork.RescueMissions.GetAsync(
                m => m.RescueRequestID == reliefOrder.RescueRequestID
                     && m.RescueTeamID == reliefOrder.RescueTeamID
                     && !m.IsDeleted);

            if (rescueMission == null)
            {
                _logger.LogWarning("[ReliefOrderService - Sql Server] Cannot find Rescue Mission for RescueRequestID: {RequestID} - RescueTeamID: {TeamID}", reliefOrder.RescueRequestID, reliefOrder.RescueTeamID);
                return null;
            }

            if (rescueMission.Status != RescueMissionSettings.INPROGRESS_STATUS)
            {
                _logger.LogWarning("[ReliefOrderService] Rescue Mission status must be InProgress. Current status: {Status}", rescueMission.Status);
                return null;
            }

            _logger.LogInformation("[ReliefOrderService] Start Transaction for Preparing Relief Order");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Set ManagerID from JWT token
                reliefOrder.ManagerID = managerID;

                // Loop through items and process inventory
                foreach (var item in request.Items)
                {
                    _logger.LogInformation("[ReliefOrderService] Processing item ReliefItemID: {ItemID} - Quantity: {Quantity}", item.ReliefItemID, item.Quantity);

                    // Check inventory
                    InventoryEntity? inventory = await _unitOfWork.Inventories.GetAsync(
                        inv => inv.ReliefItemID == item.ReliefItemID);

                    if (inventory == null)
                    {
                        _logger.LogWarning("[ReliefOrderService] Cannot find Inventory for ReliefItemID: {ItemID}", item.ReliefItemID);
                        await _unitOfWork.RollbackTransactionAsync();
                        throw new InvalidOperationException($"Cannot find inventory for ReliefItemID: {item.ReliefItemID}");
                    }

                    if (inventory.Quantity < item.Quantity)
                    {
                        _logger.LogWarning("[ReliefOrderService] Not enough stock for ReliefItemID: {ItemID}. Available: {Available}, Requested: {Requested}", item.ReliefItemID, inventory.Quantity, item.Quantity);
                        await _unitOfWork.RollbackTransactionAsync();
                        throw new InvalidOperationException($"Not enough stock for ReliefItemID: {item.ReliefItemID}. Available: {inventory.Quantity}, Requested: {item.Quantity}");
                    }

                    // Deduct inventory
                    inventory.Quantity -= item.Quantity;
                    inventory.LastUpdated = DateTime.UtcNow;
                    _unitOfWork.Inventories.Update(inventory);

                    _logger.LogInformation("[ReliefOrderService] Inventory deducted for ReliefItemID: {ItemID}. Remaining: {Remaining}", item.ReliefItemID, inventory.Quantity);

                    // Save to ReliefOrderDetails
                    var orderDetail = new ReliefOrderDetailEntity
                    {
                        ReliefOrderDetailID = Guid.NewGuid(),
                        ReliefOrderID = reliefOrder.ReliefOrderID,
                        ReliefItemID = item.ReliefItemID,
                        Quantity = item.Quantity
                    };

                    await _unitOfWork.ReliefOrderDetails.AddAsync(orderDetail);

                    _logger.LogInformation("[ReliefOrderService] ReliefOrderDetail saved for ReliefItemID: {ItemID}", item.ReliefItemID);
                }

                // Update ReliefOrder status
                reliefOrder.Status = ReliefOrderSettings.PREPARED_STATUS;
                reliefOrder.PreparedTime = DateTime.UtcNow;
                _unitOfWork.ReliefOrders.Update(reliefOrder);

                _logger.LogInformation("[ReliefOrderService] Relief Order status updated to Prepared");

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("[ReliefOrderService] Transaction committed successfully for ReliefOrderID: {ID}", reliefOrder.ReliefOrderID);

                // Kafka message after commit
                var kafkaMessage = new OrderPreparedMessage
                {
                    ReliefOrderID = reliefOrder.ReliefOrderID,
                    RescueRequestID = reliefOrder.RescueRequestID,
                    RescueTeamID = reliefOrder.RescueTeamID,
                    ManagerID = reliefOrder.ManagerID,
                    Status = reliefOrder.Status,
                    PreparedTime = reliefOrder.PreparedTime,
                    Items = request.Items.Select(i => new OrderPreparedItemMessage
                    {
                        ReliefItemID = i.ReliefItemID,
                        Quantity = i.Quantity
                    }).ToList()
                };

                await _kafkaProducer.ProduceAsync(
                    topic: KafkaSettings.ORDER_PREPARED_TOPIC,
                    key: reliefOrder.ReliefOrderID.ToString(),
                    message: kafkaMessage);

                _logger.LogInformation("[ReliefOrderService - Kafka Producer] Kafka message sent to topic {Topic}", KafkaSettings.ORDER_PREPARED_TOPIC);

                return _mapper.Map<ReliefOrderResponseDTO>(reliefOrder);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("[ReliefOrderService - Error] Cannot Prepare Relief Order with ID: {ID} - Error: {error}. Transaction RollBack", request.ReliefOrderID, ex.Message.ToString());
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}

