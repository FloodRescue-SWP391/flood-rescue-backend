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
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using FloodRescue.Services.DTO.Request.ReliefOrderRequest;
using FloodRescue.Services.BusinessModels;

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
        private const string ORDER_DETAIL_KEY_PREFIX = "relieforder:detail:";
        private const string ORDER_FILTER_PREFIX = "relieforder:filter:";


        private const string PENDING_MISSIONS_KEY_PREFIX = "rescuemission:pending:team:";
        private const string MISSION_FILTER_PREFIX = "rescuemission:filter";
        private const string MISSION_DETAIL_KEY_PREFIX = "rescuemission:detail:";

        private const string ALL_RESCUE_REQUESTS_KEY = "rescuerequest:all";
        private const string TRACK_REQUEST_KEY_PREFIX = "rescuerequest:track:";
        private const string RESCUE_REQUEST_FILTER_PREFIX = "rescuerequest:filter:";
        private const string REQUEST_DETAIL_KEY_PREFIX = "rescuerequest:detail:";

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


        public async Task<ReliefOrderResponseDTO?> CreateReliefOrderAsync(ReliefOrderRequestDTO request, Guid coordinatorID)
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

                var dispatchResult = await _rescueMissionService.DispatchMissionAsync(dispatchDto, coordinatorID);

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

                await Task.WhenAll(
                    _cacheService.RemovePatternAsync($"{PENDING_ORDERS_CACHE_KEY}"),
                    _cacheService.RemovePatternAsync($"{ORDER_DETAIL_KEY_PREFIX}"),
                    _cacheService.RemovePatternAsync($"{ORDER_FILTER_PREFIX}"),

                     _cacheService.RemoveAsync($"{PENDING_MISSIONS_KEY_PREFIX}"),
                    _cacheService.RemovePatternAsync($"{MISSION_FILTER_PREFIX}"),
                    _cacheService.RemovePatternAsync($"{MISSION_DETAIL_KEY_PREFIX}"),
                    // _cacheService.RemovePatternAsync($"*{TEAM_MEMBERS_KEY_PREFIX}*"),
                    _cacheService.RemovePatternAsync($"{TRACK_REQUEST_KEY_PREFIX}"),
                    _cacheService.RemovePatternAsync($"{RESCUE_REQUEST_FILTER_PREFIX}"),
                    _cacheService.RemovePatternAsync($"{REQUEST_DETAIL_KEY_PREFIX}"),
                    _cacheService.RemovePatternAsync($"{ALL_RESCUE_REQUESTS_KEY}")
                );

                _logger.LogInformation("[ReliefOrderService - Redis] Cleared filter list cache for prefix in rescue request {prefix1}, {prefix2}, {prefix3}, {prefix4}", TRACK_REQUEST_KEY_PREFIX, MISSION_FILTER_PREFIX, RESCUE_REQUEST_FILTER_PREFIX, REQUEST_DETAIL_KEY_PREFIX);

                _logger.LogInformation("[ReliefOrderService - Redis] Cleared filter list cache for prefix in rescue mission {prefix1}, {prefix2}, {prefix3}", MISSION_FILTER_PREFIX, MISSION_FILTER_PREFIX, MISSION_DETAIL_KEY_PREFIX);

                _logger.LogInformation("[ReliefOrderService - Redis] Cleared cache with cache key pattern {prefix1}, {prefix2}, {prefix3}", PENDING_ORDERS_CACHE_KEY, ORDER_DETAIL_KEY_PREFIX, ORDER_FILTER_PREFIX);

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

            List<OrderPreparedItemMessage> itemMessages = new List<OrderPreparedItemMessage>();
            List<ReliefItemDetailsTracking> trackingLists = new List<ReliefItemDetailsTracking>();

            try
            {
                // Set ManagerID from JWT token
                reliefOrder.ManagerID = managerID;

                // Loop through items and process inventory
                // Trong request là 1 mảng Items chứ danh sách những món Relief Item
                foreach (var item in request.Items)
                {
                    _logger.LogInformation("[ReliefOrderService] Processing item ReliefItemID: {ItemID} - Quantity: {Quantity}", item.ReliefItemID, item.Quantity);

                    // Check inventory
                    // Trong inventory có chứa warehouse nên join qua để lấy địa chỉ 
                    // Tìm trong tồn kho lấy item tương đương với 1 món Relief Item đó ra để trừ hay cộng tồn kho đó vô
                    InventoryEntity? inventory = await _unitOfWork.Inventories.GetAsync(
                        inv => inv.ReliefItemID == item.ReliefItemID, includes: inv => inv.Warehouse!);

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

                    string warehouseAddress = inventory.Warehouse!.Address!;

                    OrderPreparedItemMessage itemMessage = new OrderPreparedItemMessage
                    {
                        ReliefItemID = item.ReliefItemID,
                        Quantity = item.Quantity,
                        WarehouseAddress = warehouseAddress,
                    };

                    ReliefItemDetailsTracking trackItem = new ReliefItemDetailsTracking
                    {
                        ReliefItemID =  item.ReliefItemID,
                        Quantity = item.Quantity,
                        WarehouseAddress = warehouseAddress,
                    };

                    itemMessages.Add(itemMessage);
                    trackingLists.Add(trackItem);

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

                await Task.WhenAll(
                  _cacheService.RemovePatternAsync($"{PENDING_ORDERS_CACHE_KEY}"),
                  _cacheService.RemovePatternAsync($"{ORDER_DETAIL_KEY_PREFIX}"),
                  _cacheService.RemovePatternAsync($"{ORDER_FILTER_PREFIX}"),

                   _cacheService.RemoveAsync($"{PENDING_MISSIONS_KEY_PREFIX}"),
                  _cacheService.RemovePatternAsync($"{MISSION_FILTER_PREFIX}"),
                  _cacheService.RemovePatternAsync($"{MISSION_DETAIL_KEY_PREFIX}"),
                  // _cacheService.RemovePatternAsync($"*{TEAM_MEMBERS_KEY_PREFIX}*"),
                  _cacheService.RemovePatternAsync($"{TRACK_REQUEST_KEY_PREFIX}"),
                  _cacheService.RemovePatternAsync($"{RESCUE_REQUEST_FILTER_PREFIX}"),
                  _cacheService.RemovePatternAsync($"{REQUEST_DETAIL_KEY_PREFIX}"),
                  _cacheService.RemovePatternAsync($"{ALL_RESCUE_REQUESTS_KEY}")
              );

                _logger.LogInformation("[ReliefOrderService - Redis] Cleared filter list cache for prefix in rescue request {prefix1}, {prefix2}, {prefix3}, {prefix4}", TRACK_REQUEST_KEY_PREFIX, MISSION_FILTER_PREFIX, RESCUE_REQUEST_FILTER_PREFIX, REQUEST_DETAIL_KEY_PREFIX);

                _logger.LogInformation("[ReliefOrderService - Redis] Cleared filter list cache for prefix in rescue mission {prefix1}, {prefix2}, {prefix3}", MISSION_FILTER_PREFIX, MISSION_FILTER_PREFIX, MISSION_DETAIL_KEY_PREFIX);

                _logger.LogInformation("[ReliefOrderService - Redis] Cleared cache with cache key pattern {prefix1}, {prefix2}, {prefix3}", PENDING_ORDERS_CACHE_KEY, ORDER_DETAIL_KEY_PREFIX, ORDER_FILTER_PREFIX);

                // Kafka message after commit
                var kafkaMessage = new OrderPreparedMessage
                {
                    ReliefOrderID = reliefOrder.ReliefOrderID,
                    RescueRequestID = reliefOrder.RescueRequestID,
                    RescueTeamID = reliefOrder.RescueTeamID,
                    ManagerID = reliefOrder.ManagerID,
                    Status = reliefOrder.Status,
                    PreparedTime = reliefOrder.PreparedTime,
                    Items = itemMessages,
                };

                await _kafkaProducer.ProduceAsync(
                    topic: KafkaSettings.ORDER_PREPARED_TOPIC,
                    key: reliefOrder.ReliefOrderID.ToString(),
                    message: kafkaMessage);

                _logger.LogInformation("[ReliefOrderService - Kafka Producer] Kafka message sent to topic {Topic}", KafkaSettings.ORDER_PREPARED_TOPIC);

                ReliefOrderResponseDTO response = _mapper.Map<ReliefOrderResponseDTO>(reliefOrder);

                response.ItemTrackings = trackingLists;

                return response;
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

        public async Task<ReliefOrderDetailResponseDTO?> GetOrderDetailAsync(Guid reliefOrderId)
        {
            _logger.LogInformation("[ReliefOrderService] GetOrderDetail called for ReliefOrderID: {ID}", reliefOrderId);

            // Check cache
            string cacheKey = $"{ORDER_DETAIL_KEY_PREFIX}{reliefOrderId}";

            var cached = await _cacheService.GetAsync<ReliefOrderDetailResponseDTO>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("[ReliefOrderService - Redis] Cache hit for order detail. ReliefOrderID: {ID}", reliefOrderId);
                return cached;
            }

            _logger.LogInformation("[ReliefOrderService - Redis] Cache miss for order detail. Querying database. ReliefOrderID: {ID}", reliefOrderId);

            // Query ReliefOrder theo ID, Include RescueTeam để lấy TeamName
            ReliefOrderEntity? reliefOrder = await _unitOfWork.ReliefOrders.GetAsync(
                (ReliefOrderEntity ro) => ro.ReliefOrderID == reliefOrderId && !ro.IsDeleted,
                ro => ro.RescueTeam!);

            if (reliefOrder == null)
            {
                _logger.LogWarning("[ReliefOrderService - Sql Server] ReliefOrder with ID: {ID} not found", reliefOrderId);
                return null;
            }

            _logger.LogInformation("[ReliefOrderService - Sql Server] Found ReliefOrder {ID} with status {Status}", reliefOrderId, reliefOrder.Status);

            // Query ReliefOrderDetails với Include nested:
            // ReliefOrderDetail -> ReliefItem -> Category
            // ReliefOrderDetail -> ReliefItem -> Unit (bảng mới tách từ DB)
            List<ReliefOrderDetailEntity> orderDetails = await _unitOfWork.ReliefOrderDetails.GetQueryable()
                .Where(d => d.ReliefOrderID == reliefOrderId)
                .Include(d => d.ReliefItem!)
                    .ThenInclude(i => i.Category!)
                .Include(d => d.ReliefItem!)
                    .ThenInclude(i => i.Unit!)
                .AsNoTracking()
                .ToListAsync();

            _logger.LogInformation("[ReliefOrderService - Sql Server] Found {Count} item(s) in ReliefOrder {ID}", orderDetails.Count, reliefOrderId);

            // Mapping sang DTO
            ReliefOrderDetailResponseDTO result = new()
            {
                ReliefOrderID = reliefOrder.ReliefOrderID,
                Status = reliefOrder.Status,
                CreatedTime = reliefOrder.CreatedTime,
                PreparedTime = reliefOrder.PreparedTime,
                PickedUpTime = reliefOrder.PickedUpTime,
                AssignedTeamID = reliefOrder.RescueTeamID,
                TeamName = reliefOrder.RescueTeam?.TeamName,
                Description = reliefOrder.Description,
                Items = orderDetails.Select(d => new ReliefOrderItemDTO
                {
                    ItemID = d.ReliefItemID,
                    ItemName = d.ReliefItem?.ReliefItemName ?? "Unknown",
                    CategoryName = d.ReliefItem?.Category?.CategoryName ?? "Unknown",
                    UnitName = d.ReliefItem?.Unit?.UnitName ?? "Unknown",
                    Quantity = d.Quantity
                }).ToList()
            };

            // Cache kết quả
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            _logger.LogInformation("[ReliefOrderService - Redis] Cached order detail for ReliefOrderID: {ID}", reliefOrderId);

            return result;
        }
        public async Task<PagedResult<ReliefOrderListResponseDTO>> GetFilteredOrdersAsync(ReliefOrderFilterDTO filter)
        {
            _logger.LogInformation("[ReliefOrderService] GetFilteredOrders called. Statuses: {Statuses}, Page: {Page}, Size: {Size}",
                filter.Statuses != null ? string.Join(",", filter.Statuses) : "All",
                filter.PageNumber, filter.PageSize);

            // Check cache
            string cacheKey = BuildOrderFilterCacheKey(filter);

            PagedResult<ReliefOrderListResponseDTO>? cached = await _cacheService.GetAsync<PagedResult<ReliefOrderListResponseDTO>>(cacheKey);

            if (cached != null)
            {
                _logger.LogInformation("[ReliefOrderService - Redis] Cache hit for filter key: {Key}. TotalCount: {Count}", cacheKey, cached.TotalCount);
                return cached;
            }

            _logger.LogInformation("[ReliefOrderService - Redis] Cache miss for filter key: {Key}. Querying database.", cacheKey);

            // Khởi tạo query bằng GetQueryable - chưa chạy xuống DB
            IQueryable<ReliefOrderEntity> query = _unitOfWork.ReliefOrders.GetQueryable();

            query = query.Where(ro => !ro.IsDeleted);

            // Lọc theo mảng Statuses (Multi-select filter)
            if (filter.Statuses != null && filter.Statuses.Count > 0)
            {
                query = query.Where(ro => filter.Statuses.Contains(ro.Status));
            }

            // Lọc theo mốc CreatedTime
            if (filter.CreatedFromDate.HasValue)
            {
                query = query.Where(ro => ro.CreatedTime >= filter.CreatedFromDate.Value);
            }

            if (filter.CreatedToDate.HasValue)
            {
                query = query.Where(ro => ro.CreatedTime <= filter.CreatedToDate.Value);
            }

            // Lọc theo mốc PreparedTime
            if (filter.PreparedFromDate.HasValue)
            {
                query = query.Where(ro => ro.PreparedTime.HasValue && ro.PreparedTime.Value >= filter.PreparedFromDate.Value);
            }

            if (filter.PreparedToDate.HasValue)
            {
                query = query.Where(ro => ro.PreparedTime.HasValue && ro.PreparedTime.Value <= filter.PreparedToDate.Value);
            }

            // Lọc theo mốc PickedUpTime
            if (filter.PickedUpFromDate.HasValue)
            {
                query = query.Where(ro => ro.PickedUpTime.HasValue && ro.PickedUpTime.Value >= filter.PickedUpFromDate.Value);
            }

            if (filter.PickedUpToDate.HasValue)
            {
                query = query.Where(ro => ro.PickedUpTime.HasValue && ro.PickedUpTime.Value <= filter.PickedUpToDate.Value);
            }

            // Tính tổng số dòng cho FE làm thanh phân trang
            int totalCount = await query.CountAsync();

            _logger.LogInformation("[ReliefOrderService - Sql Server] Total {Count} relief order(s) matched filter", totalCount);

            // Sắp xếp ưu tiên đơn mới nhất + Include RescueTeam + phân trang
            List<ReliefOrderEntity> entities = await query
                .OrderByDescending(ro => ro.CreatedTime)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Include(ro => ro.RescueTeam)
                .AsNoTracking()
                .ToListAsync();

            // Lấy danh sách OrderIDs để đếm TotalItems từ ReliefOrderDetails
            List<Guid> orderIds = entities.Select(ro => ro.ReliefOrderID).ToList();

            List<ReliefOrderDetailEntity> allDetails = await _unitOfWork.ReliefOrderDetails.GetAllAsync(
                (ReliefOrderDetailEntity d) => orderIds.Contains(d.ReliefOrderID));

            // Group by OrderID để đếm tổng số loại hàng
            var totalItemsByOrder = allDetails
                .GroupBy(d => d.ReliefOrderID)
                .ToDictionary(g => g.Key, g => g.Count());

            // Mapping sang DTO
            List<ReliefOrderListResponseDTO> dtos = entities.Select(ro => new ReliefOrderListResponseDTO
            {
                ReliefOrderID = ro.ReliefOrderID,
                Status = ro.Status,
                CreatedTime = ro.CreatedTime,
                PreparedTime = ro.PreparedTime,
                PickedUpTime = ro.PickedUpTime,
                AssignedTeamID = ro.RescueTeamID,
                TeamName = ro.RescueTeam?.TeamName,
                TotalItems = totalItemsByOrder.GetValueOrDefault(ro.ReliefOrderID, 0)
            }).ToList();

            // Đóng gói và cache
            PagedResult<ReliefOrderListResponseDTO> result = new()
            {
                Data = dtos,
                TotalCount = totalCount
            };

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            _logger.LogInformation("[ReliefOrderService - Redis] Cached filter result. Key: {Key}, DataCount: {Count}, TotalCount: {Total}", cacheKey, dtos.Count, totalCount);

            return result;
        }

        private string BuildOrderFilterCacheKey(ReliefOrderFilterDTO filter)
        {
            string statusKey = filter.Statuses != null && filter.Statuses.Count > 0
                ? string.Join(",", filter.Statuses.OrderBy(s => s))
                : "";

            return $"{ORDER_FILTER_PREFIX}s={statusKey}" +
                   $"|cf={filter.CreatedFromDate:yyyyMMdd}|ct={filter.CreatedToDate:yyyyMMdd}" +
                   $"|pf={filter.PreparedFromDate:yyyyMMdd}|pt={filter.PreparedToDate:yyyyMMdd}" +
                   $"|uf={filter.PickedUpFromDate:yyyyMMdd}|ut={filter.PickedUpToDate:yyyyMMdd}" +
                   $"|p={filter.PageNumber}|ps={filter.PageSize}";
        }
    }
}

