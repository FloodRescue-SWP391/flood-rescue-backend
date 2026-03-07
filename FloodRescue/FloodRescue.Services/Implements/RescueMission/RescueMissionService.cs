using AutoMapper;
using Azure;
using FloodRescue.Repositories.Entites;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.DTO.Kafka;
using FloodRescue.Services.DTO.Request.RescueMissionRequest;
using FloodRescue.Services.DTO.Response.RescueMissionResponse;
using FloodRescue.Services.DTO.SignalR;
using FloodRescue.Services.Interface.Kafka;
using FloodRescue.Services.Interface.RealTimeNoti;
using FloodRescue.Services.Interface.RescueMission;
using FloodRescue.Services.SharedSetting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using RescueMissionEntity = FloodRescue.Repositories.Entites.RescueMission;
using RescueRequestEntity = FloodRescue.Repositories.Entites.RescueRequest;
using RescueTeamEntity = FloodRescue.Repositories.Entites.RescueTeam;
using RescueTeamMemberEntity = FloodRescue.Repositories.Entites.RescueTeamMember;
using ReliefOrderEntity = FloodRescue.Repositories.Entites.ReliefOrder;
using FloodRescue.Services.Interface.Cache;
using FloodRescue.Services.BusinessModels;
using Microsoft.EntityFrameworkCore;
using FloodRescue.Services.DTO.Cache;

namespace FloodRescue.Services.Implements.RescueMission
{
    public class RescueMissionService : IRescueMissionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RescueMissionService> _logger;
        private readonly IKafkaProducerService _kafkaProducer;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;

        // Cache keys
        private const string PENDING_MISSIONS_KEY_PREFIX = "rescuemission:pending:team:";

        private const string MISSION_FILTER_PREFIX = "rescuemission:filter";

        public RescueMissionService(IUnitOfWork unitOfWork,
            ILogger<RescueMissionService> logger,
            IKafkaProducerService kafkaProducer,
           IMapper mapper, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _kafkaProducer = kafkaProducer;
            _mapper = mapper;
            _cacheService = cacheService;

        }

        public async Task<DispatchMissionResponseDTO?> DispatchMissionAsync(DispatchMissionRequestDTO request, Guid coordinatorID)
        {

            _logger.LogInformation("[RescueMissionService] Starting Dispatch with Request ID: {RequestID}, Team ID: {TeamID}", request.RescueRequestID, request.RescueTeamID);

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                //Sửa lại pending
                //Tìm ra Rescue Request chính xác và đang ở trạng thái "Pending" và lấy được status
                RescueRequestEntity? rescueRequest = await _unitOfWork.RescueRequests.GetAsync((RescueRequestEntity rr) => rr.RescueRequestID == request.RescueRequestID && rr.Status == RescueRequestSettings.PENDING_STATUS && !rr.IsDeleted);

                if (rescueRequest == null)
                {
                    _logger.LogWarning("[RescueMissionService - Sql Server] Rescue Request with ID: {RequestID} not found or not in Pending status", request.RescueRequestID);
                    return null;
                }

                //Coi rescue team id có tồn tại và đang ở trạng thái "Available" 
                RescueTeamEntity? rescueTeam = await _unitOfWork.RescueTeams.GetAsync((RescueTeamEntity rt) => rt.RescueTeamID == request.RescueTeamID && rt.CurrentStatus == RescueTeamSettings.AVAILABLE_STATUS && !rt.IsDeleted);

                if (rescueTeam == null)
                {
                    _logger.LogWarning("[RescueMissionService - Sql Server] Rescue Team with ID: {TeamID} not found or not Available", request.RescueTeamID);
                    return null;
                }

                //Cập nhật trạng thái của Rescue Request thành "Processing"
                rescueRequest.Status = RescueMissionSettings.PROCESSING_STATUS;

                //Tạo mới Rescue Mission
                RescueMissionEntity newMission = new RescueMissionEntity
                {
                    RescueMissionID = Guid.NewGuid(),
                    RescueRequestID = request.RescueRequestID,
                    RescueTeamID = request.RescueTeamID,
                    CoordinatorID = coordinatorID,
                    Status = RescueMissionSettings.ASSIGNED_STATUS,
                    AssignedAt = DateTime.UtcNow,
                    StartTime = null,
                    EndTime = null,
                    IsDeleted = false

                };

                await _unitOfWork.RescueMissions.AddAsync(newMission);

                //Cập nhật trạng thái của Rescue Team thành "Busy"
                rescueTeam.CurrentStatus = RescueTeamSettings.BUSY_STATUS;


                int saveResult = await _unitOfWork.SaveChangesAsync();

                if (saveResult <= 0)
                {
                    _logger.LogError("[RescueMissionService - Error] SaveChanges returned 0 rows during dispatch. RequestID: {RequestID}, TeamID: {TeamID}", request.RescueRequestID, request.RescueTeamID);
                    await _unitOfWork.RollbackTransactionAsync();  
                    return null;
                }

                //gửi mesage qua kafka
                MissionAssignedMessage kafkaMessage = _mapper.Map<MissionAssignedMessage>(rescueRequest);

                _mapper.Map(rescueTeam, kafkaMessage);

                _mapper.Map(newMission, kafkaMessage);

                await _kafkaProducer.ProduceAsync(topic: KafkaSettings.MISSION_ASSIGN_TOPIC, key: newMission.RescueMissionID.ToString(), message: kafkaMessage);

                _logger.LogInformation("[RescueMissionService - Kafka Producer] Kafka message sent to topic {Topic}", KafkaSettings.MISSION_ASSIGN_TOPIC);               

                _logger.LogInformation("[RescueMissionService] Successfully dispatched mission with ID: {MissionID} for Request ID: {RequestID} and Team ID: {TeamID}", newMission.RescueMissionID, request.RescueRequestID, request.RescueTeamID);

                DispatchMissionResponseDTO response = _mapper.Map<DispatchMissionResponseDTO>(newMission);

                _mapper.Map(rescueRequest, response);

                _mapper.Map(rescueTeam, response);

                response.Message = $"Rescue mission dispatched successfully for Team {rescueTeam.RescueTeamID} - Team Name {rescueTeam.TeamName}.";

                await _unitOfWork.CommitTransactionAsync();

                await _cacheService.RemoveAsync($"{PENDING_MISSIONS_KEY_PREFIX}{request.RescueTeamID}");

                await _cacheService.RemovePatternAsync($"*{MISSION_FILTER_PREFIX}*");

                _logger.LogInformation("[RescueMissionService - Redis] Cleared filter list cache for prefix {prefix}", MISSION_FILTER_PREFIX);

                _logger.LogInformation("[RescueMissionService - Redis] Cleared pending missions cache for TeamID: {TeamID}", request.RescueTeamID);
                return response;
                
            }
            catch(Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                _logger.LogError(ex, "[RescueMissionService - Error] Dispatch failed. Transaction rolled back. RequestID: {RequestID}", request.RescueRequestID);

                throw;
            }
        }

        public async Task<(List<PendingMissionResponseDTO>? Data, string? ErrorMessage)> GetPendingMissionsAsync(Guid currentUserId)
        {
            _logger.LogInformation("[RescueMissionService] GetPendingMissions called for UserID: {UserID}", currentUserId);
            // 1. Kiểm tra User có phải thuộc RescueTeamMember không
            RescueTeamMemberEntity? teamMember = await _unitOfWork.RescueTeamMembers.GetAsync(m => m.UserID == currentUserId && !m.IsDeleted);
            if (teamMember == null)
            {
                _logger.LogWarning("[RescueMissionService - Sql Server] User {UserID} is not a member of any rescue team.", currentUserId);
                return (null, "User không thuộc đội cứu hộ nào");
            }

            Guid teamId = teamMember.RescueTeamID;
            _logger.LogInformation("[RescueMissionService] User {UserID} belongs to TeamID: {TeamID}", currentUserId, teamId);

            // 2. Check cache first
            string cacheKey = $"{PENDING_MISSIONS_KEY_PREFIX}{teamId}";
            var cached = await _cacheService.GetAsync<List<PendingMissionResponseDTO>>(cacheKey);

            if (cached != null)
            {
                _logger.LogInformation("[RescueMissionService - Redis] Cache hit for pending missions TeamID: {TeamID}. Count: {Count}", teamId, cached.Count);
                return (cached, null);
            }
            _logger.LogInformation("[RescueMissionService - Redis] Cache miss. Querying DB for pending missions TeamID: {TeamID}", teamId);
            // 3. Query: Lấy các mission đang ở trạng thái "Assigned" của team này
            List<RescueMissionEntity> pendingMissions = await _unitOfWork.RescueMissions.GetAllAsync(
                    filter: m => m.RescueTeamID == teamId
                    && m.Status == RescueMissionSettings.ASSIGNED_STATUS
                    && !m.IsDeleted,
                    includes: m => m.RescueRequest! // Include RescueRequest để lấy thông tin chi tiết
                );

            if (pendingMissions == null || !pendingMissions.Any())
            {
                _logger.LogInformation("[RescueMissionService] No pending missions found for TeamID: {TeamID}", teamId);
                return (new List<PendingMissionResponseDTO>(), null);
            }
            // 4. Lấy danh sách RescueRequestIDs để query images
            var requestIds = pendingMissions
                .Where(m => m.RescueRequest != null)
                .Select(m => m.RescueRequestID)
                .ToList();

            // 5. Query images cho tất cả requests (tối ưu - chỉ 1 query)
            var allImages = await _unitOfWork.RescueRequestImages.GetAllAsync(
                img => requestIds.Contains(img.RescueRequestID)
            );

            // 6. Group images theo RescueRequestID
            var imagesByRequest = allImages
                .GroupBy(img => img.RescueRequestID)
                .ToDictionary(g => g.Key, g => g.Select(img => img.ImageUrl).ToList());

            // 7. Mapping sang DTO
            List<PendingMissionResponseDTO> result = pendingMissions.Select(mission => new PendingMissionResponseDTO
            {
                // Mission Info
                RescueMissionID = mission.RescueMissionID,
                AssignedAt = mission.AssignedAt,
                MissionStatus = mission.Status,

                // Request Info
                RescueRequestID = mission.RescueRequestID,
                ShortCode = mission.RescueRequest?.ShortCode ?? string.Empty,
                RequestType = mission.RescueRequest?.RequestType ?? string.Empty,
                Description = mission.RescueRequest?.Description,

                // Citizen Info
                CitizenName = mission.RescueRequest?.CitizenName,
                CitizenPhone = mission.RescueRequest?.CitizenPhone ?? string.Empty,
                PeopleCount = mission.RescueRequest?.PeopleCount ?? 0,

                // Location Info
                Address = mission.RescueRequest?.Address,
                LocationLatitude = mission.RescueRequest?.LocationLatitude ?? 0,
                LocationLongitude = mission.RescueRequest?.LocationLongitude ?? 0,

                // Images
                ImageUrls = imagesByRequest.TryGetValue(mission.RescueRequestID, out var urls) ? urls : new List<string>(),

                // Timestamps
                RequestCreatedTime = mission.RescueRequest?.CreatedTime ?? DateTime.MinValue
            }).ToList();

            // 8. Cache the result
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            _logger.LogInformation("[RescueMissionService - Redis] Cached {Count} pending missions for TeamID: {TeamID}", result.Count, teamId);
            return (result, null);
        }

        public async Task<RespondMissionResponseDTO?> RespondMissionAsync(RespondMessageRequestDTO request)
        {
            _logger.LogInformation("[RescueMissionService] Starting Respond with MissionID: {MissionID}, IsAccepted: {IsAccepted}", request.RescueMissionID, request.IsAccepted);

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                //Tìm request đang pending
                RescueMissionEntity? rescueMission = await _unitOfWork.RescueMissions.GetAsync((RescueMissionEntity rm) => rm.RescueMissionID == request.RescueMissionID && rm.Status == RescueMissionSettings.ASSIGNED_STATUS && !rm.IsDeleted, rm => rm.RescueTeam!, rm => rm.RescueRequest!);

                if (rescueMission == null)
                {
                    _logger.LogWarning("[RescueMissionService - Sql Server] Rescue Mission with ID: {MissionID} not found or not in Assigned status", request.RescueMissionID);
                    return null;
                }

                RescueTeamEntity rescueTeam = rescueMission.RescueTeam!;    
                RescueRequestEntity rescueRequest = rescueMission.RescueRequest!;   

                DateTime respondedAt = DateTime.UtcNow;

                if (request.IsAccepted)
                {
                    rescueMission.Status = RescueMissionSettings.INPROGRESS_STATUS;    
                    rescueMission.StartTime = respondedAt;

                    //Sau khi đội cứu hộ đồng ý thì cập nhật vị trí của rescue team - 1 vị trí cố định vào database

                    var cacheLocation = await _cacheService.GetAsync<TeamLocationCacheDTO>($"Track:TeamLocation:{rescueTeam.RescueTeamID}");

                    if(cacheLocation != null)
                    {
                        rescueTeam.CurrentLatitude = cacheLocation.Latitude;
                        rescueTeam.CurrentLongitude = cacheLocation.Longitude;
                        _logger.LogInformation("[RescueMissionService] Updated RescueTeam {TeamID} location to Latitude: {Latitude}, Longitude: {Longitude} from cache after accepting mission", rescueTeam.RescueTeamID, cacheLocation.Latitude, cacheLocation.Longitude);
                    }
                    
                    _logger.LogInformation("[RescueMissionService] Rescue Mission with ID: {MissionID} accepted and set to InProgress - Team {TeamName}", request.RescueMissionID, rescueTeam.TeamName);

                   

                    // mappper rescue mission -> team accept message
                    // mapper rescue request -> team accept message 
                    // mapper rescue team -> team accept message    
                    TeamAcceptedMessage kafkaMessage = _mapper.Map<TeamAcceptedMessage>(rescueMission);
                    _mapper.Map(rescueRequest, kafkaMessage);
                    _mapper.Map(rescueTeam, kafkaMessage);

                    //gán field ngoài mapper
                    kafkaMessage.AcceptedAt = respondedAt;  

                    await _kafkaProducer.ProduceAsync(topic: KafkaSettings.TEAM_ACCEPTED_TOPIC, key: rescueMission.RescueMissionID.ToString(), message: kafkaMessage);
                    
                    _logger.LogInformation("[RescueMissionService - Kafka Producer] Kafka message sent to topic {Topic}", KafkaSettings.TEAM_ACCEPTED_TOPIC);

                }
                else
                {
                    //mapper rescue mission -> team reject message  
                    //mapper rescue request -> team reject message  
                    //mapper rescue team -> team reject message
                  
                    rescueMission.Status = RescueMissionSettings.DECLINED_STATUS;
                    rescueRequest.Status = RescueRequestSettings.PENDING_STATUS;   
                    rescueTeam.CurrentStatus = RescueTeamSettings.AVAILABLE_STATUS;

                    _logger.LogInformation("[RescueMissionService] Rescue Mission with ID: {MissionID} declined - Team {TeamName} set to Available, Request set to Pending", request.RescueMissionID, rescueTeam.TeamName);

                    TeamRejectedMessage kafkaMessage = _mapper.Map<TeamRejectedMessage>(rescueMission);
                    _mapper.Map(rescueRequest, kafkaMessage);
                    _mapper.Map(rescueTeam, kafkaMessage);

                    await _kafkaProducer.ProduceAsync(topic: KafkaSettings.TEAM_REJECTED_TOPIC, key: rescueMission.RescueMissionID.ToString(), message: kafkaMessage);
                    
                    _logger.LogInformation("[RescueMissionService - Kafka Producer] Kafka message sent to topic {Topic}", KafkaSettings.TEAM_REJECTED_TOPIC);
                }

                int saveResult = await _unitOfWork.SaveChangesAsync();

                if (saveResult <= 0)
                {
                    _logger.LogError("[RescueMissionService - Error] SaveChanges returned 0 rows during respond. MissionID: {MissionID}", request.RescueMissionID);  
                    await _unitOfWork.RollbackTransactionAsync();
                    return null;
                }

               

                _logger.LogInformation("[RescueMissionService] Successfully responded to mission with ID: {MissionID}", request.RescueMissionID);

                //mapper rescue misson -> respond mission response dto
                //mapper rescue request -> respond mission response dto 
                //mapper rescue team -> respond mission response dto    
                //mappper respond mission requesst dto -> respond mission response dto - map tay 


                RespondMissionResponseDTO response = _mapper.Map<RespondMissionResponseDTO>(rescueMission);
                _mapper.Map(rescueRequest, response);
                _mapper.Map(rescueTeam, response);

                response.RespondedAt = respondedAt; 
                response.Message = request.IsAccepted ? $"Rescue mission with ID {rescueMission.RescueMissionID} has been accepted by Team {rescueTeam.TeamName}." : $"Rescue mission with ID {rescueMission.RescueMissionID} has been declined by Team {rescueTeam.TeamName}.";

                await _unitOfWork.CommitTransactionAsync();

                await _cacheService.RemoveAsync($"{PENDING_MISSIONS_KEY_PREFIX}{rescueTeam.RescueTeamID}");
             
                _logger.LogInformation("[RescueMissionService - Redis] Cleared pending missions cache for TeamID: {TeamID}", rescueTeam.RescueTeamID);

                await _cacheService.RemovePatternAsync($"*{MISSION_FILTER_PREFIX}*");

                _logger.LogInformation("[RescueMissionService - Redis] Cleared filter list cache for prefix {prefix}", MISSION_FILTER_PREFIX);

                return response;

            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "[RescueMissionService - Error] Respond failed. Transaction rolled back. MissionID: {MissionID}", request.RescueMissionID);
                throw;
            }
        }

        public async Task<CompleteMissionResponseDTO?> CompleteMissionAsync(CompleteMissionRequestDTO request)
        {
            _logger.LogInformation("[RescueMissionService] Starting CompleteMission with MissionID: {MissionID}", request.RescueMissionID);

            // Lấy RescueMission từ DB kèm theo RescueRequest và RescueTeam
            RescueMissionEntity? rescueMission = await _unitOfWork.RescueMissions.GetAsync(
                (RescueMissionEntity rm) => rm.RescueMissionID == request.RescueMissionID && !rm.IsDeleted,
                rm => rm.RescueTeam!,
                rm => rm.RescueRequest!);

            if (rescueMission == null || rescueMission.Status != RescueMissionSettings.INPROGRESS_STATUS)
            {
                _logger.LogWarning("[RescueMissionService - Sql Server] RescueMission with ID: {MissionID} not found or not in InProgress status", request.RescueMissionID);
                return null;
            }

            RescueTeamEntity rescueTeam = rescueMission.RescueTeam!;
            RescueRequestEntity rescueRequest = rescueMission.RescueRequest!;

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                DateTime completedTime = DateTime.UtcNow;

                // Update RescueMission: Status = Completed, EndTime
                rescueMission.Status = RescueMissionSettings.COMPLETED_STATUS;
                rescueMission.EndTime = completedTime;

                _logger.LogInformation("[RescueMissionService] RescueMission {MissionID} set to Completed at {EndTime}", request.RescueMissionID, completedTime);

                // Update RescueTeam: CurrentStatus = Available
                rescueTeam.CurrentStatus = RescueTeamSettings.AVAILABLE_STATUS;

                var cachedLocation = await _cacheService.GetAsync<TeamLocationCacheDTO>($"Tracking:TeamLocation:{rescueTeam.RescueTeamID}");
                if (cachedLocation != null)
                {
                    rescueTeam.CurrentLatitude = cachedLocation.Latitude;
                    rescueTeam.CurrentLongitude = cachedLocation.Longitude;
                    _logger.LogInformation("[RescueMissionService - Redis] Snapshot team end location from Redis. TeamID: {TeamID}, Lat: {Lat}, Lng: {Lng}",
                        rescueTeam.RescueTeamID, cachedLocation.Latitude, cachedLocation.Longitude);
                }

                _logger.LogInformation("[RescueMissionService] RescueTeam {TeamID} - {TeamName} set to Available", rescueTeam.RescueTeamID, rescueTeam.TeamName);

                // Update RescueRequest: nếu Type == Supply thì Status = Delivered, còn lại = Completed
                if (rescueRequest.RequestType == RescueRequestType.SUPPLY_TYPE)
                {
                    rescueRequest.Status = RescueRequestSettings.DELIVERED_STATUS;
                    _logger.LogInformation("[RescueMissionService] RescueRequest {RequestID} is Supply type - set to Delivered", rescueRequest.RescueRequestID);
                }
                else
                {
                    rescueRequest.Status = RescueRequestSettings.COMPLETED_STATUS;
                    _logger.LogInformation("[RescueMissionService] RescueRequest {RequestID} is Rescue type - set to Completed", rescueRequest.RescueRequestID);
                }

                int saveResult = await _unitOfWork.SaveChangesAsync();

                if (saveResult <= 0)
                {
                    _logger.LogError("[RescueMissionService - Error] SaveChanges returned 0 rows during complete mission. MissionID: {MissionID}", request.RescueMissionID);
                    await _unitOfWork.RollbackTransactionAsync();
                    return null;
                }

                await _unitOfWork.CommitTransactionAsync();

                await _cacheService.RemovePatternAsync($"*{MISSION_FILTER_PREFIX}*");

                _logger.LogInformation("[RescueMissionService - Redis] Cleared filter list cache for prefix {prefix}", MISSION_FILTER_PREFIX);

                _logger.LogInformation("[RescueMissionService] Transaction committed for CompleteMission. MissionID: {MissionID}", request.RescueMissionID);

                // Gửi message qua Kafka
                MissionCompletedMessage kafkaMessage = new MissionCompletedMessage
                {
                    RescueMissionID = rescueMission.RescueMissionID,
                    RescueRequestID = rescueRequest.RescueRequestID,
                    RequestShortCode = rescueRequest.ShortCode,
                    RequestType = rescueRequest.RequestType,
                    RescueTeamID = rescueTeam.RescueTeamID,
                    TeamName = rescueTeam.TeamName,
                    MissionStatus = rescueMission.Status,
                    RequestStatus = rescueRequest.Status,
                    EndTime = completedTime,
                    CoordinatorID = rescueMission.CoordinatorID
                };

                await _kafkaProducer.ProduceAsync(
                    topic: KafkaSettings.MISSION_COMPLETED_TOPIC,
                    key: rescueMission.RescueMissionID.ToString(),
                    message: kafkaMessage);

                _logger.LogInformation("[RescueMissionService - Kafka Producer] Kafka message sent to topic {Topic} for MissionID: {MissionID}", KafkaSettings.MISSION_COMPLETED_TOPIC, request.RescueMissionID);

                _logger.LogInformation("[RescueMissionService] Successfully completed mission with ID: {MissionID}", request.RescueMissionID);

                // Tạo response DTO
                CompleteMissionResponseDTO response = new CompleteMissionResponseDTO
                {
                    RescueMissionID = rescueMission.RescueMissionID,
                    RescueRequestID = rescueRequest.RescueRequestID,
                    RequestShortCode = rescueRequest.ShortCode,
                    RescueTeamID = rescueTeam.RescueTeamID,
                    TeamName = rescueTeam.TeamName,
                    NewMissionStatus = rescueMission.Status,
                    NewRequestStatus = rescueRequest.Status,
                    NewTeamStatus = rescueTeam.CurrentStatus,
                    EndTime = completedTime,
                    Message = $"Rescue mission {rescueMission.RescueMissionID} has been completed by Team {rescueTeam.TeamName}. Team is now available for new missions."
                };

                return response;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "[RescueMissionService - Error] CompleteMission failed. Transaction rolled back. MissionID: {MissionID}", request.RescueMissionID);
                throw;
            }
        }

        public async Task<ConfirmPickupResponseDTO?> ConfirmPickupAsync(ConfirmPickUpRequestDTO request)
        {
            _logger.LogInformation("[RescueMissionService] Starting ConfirmPickup with MissionID: {MissionID}, ReliefOrderID: {OrderID}", request.RescueMissionID, request.ReliefOrderID);

            // Lấy ReliefOrder từ DB theo ID
            ReliefOrderEntity? reliefOrder = await _unitOfWork.ReliefOrders.GetAsync(
                (ReliefOrderEntity ro) => ro.ReliefOrderID == request.ReliefOrderID && !ro.IsDeleted);

            if (reliefOrder == null || reliefOrder.Status != ReliefOrderSettings.PREPARED_STATUS)
            {
                _logger.LogWarning("[RescueMissionService - Sql Server] ReliefOrder with ID: {OrderID} not found or not in Prepared status", request.ReliefOrderID);
                return null;
            }

            // Lấy RescueMission từ DB
            RescueMissionEntity? rescueMission = await _unitOfWork.RescueMissions.GetAsync(
                (RescueMissionEntity rm) => rm.RescueMissionID == request.RescueMissionID && !rm.IsDeleted,
                rm => rm.RescueTeam!,
                rm => rm.RescueRequest!);

            if (rescueMission == null || rescueMission.Status != RescueMissionSettings.INPROGRESS_STATUS)
            {
                _logger.LogWarning("[RescueMissionService - Sql Server] RescueMission with ID: {MissionID} not found or not in InProgress status", request.RescueMissionID);
                return null;
            }

            // Check ReliefOrder.RescueRequestID phải trùng với RescueMission.RescueRequestID
            if (reliefOrder.RescueRequestID != rescueMission.RescueRequestID)
            {
                _logger.LogWarning("[RescueMissionService] ReliefOrder.RescueRequestID ({OrderRequestID}) does not match RescueMission.RescueRequestID ({MissionRequestID})",
                    reliefOrder.RescueRequestID, rescueMission.RescueRequestID);
                return null;
            }

            RescueTeamEntity rescueTeam = rescueMission.RescueTeam!;
            RescueRequestEntity rescueRequest = rescueMission.RescueRequest!;

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                DateTime pickedUpTime = DateTime.UtcNow;

                // Update ReliefOrder: Status = PickedUp, PickedUpTime
                reliefOrder.Status = ReliefOrderSettings.PICKEDUP_STATUS;
                reliefOrder.PickedUpTime = pickedUpTime;

                _logger.LogInformation("[RescueMissionService] ReliefOrder {OrderID} set to PickedUp at {PickedUpTime}", request.ReliefOrderID, pickedUpTime);

                int saveResult = await _unitOfWork.SaveChangesAsync();

                if (saveResult <= 0)
                {
                    _logger.LogError("[RescueMissionService - Error] SaveChanges returned 0 rows during confirm pickup. OrderID: {OrderID}", request.ReliefOrderID);
                    await _unitOfWork.RollbackTransactionAsync();
                    return null;
                }

                await _unitOfWork.CommitTransactionAsync();

                // Gửi message qua Kafka
                DeliveryStartedMessage kafkaMessage = new DeliveryStartedMessage
                {
                    ReliefOrderID = reliefOrder.ReliefOrderID,
                    RescueMissionID = rescueMission.RescueMissionID,
                    RescueRequestID = rescueMission.RescueRequestID,
                    RescueTeamID = rescueTeam.RescueTeamID,
                    TeamName = rescueTeam.TeamName,
                    OrderStatus = reliefOrder.Status,
                    PickedUpTime = pickedUpTime,
                    CoordinatorID = rescueMission.CoordinatorID
                };

                await _kafkaProducer.ProduceAsync(
                    topic: KafkaSettings.DELIVERY_STARTED_TOPIC,
                    key: reliefOrder.ReliefOrderID.ToString(),
                    message: kafkaMessage);

                _logger.LogInformation("[RescueMissionService - Kafka Producer] Kafka message sent to topic {Topic}", KafkaSettings.DELIVERY_STARTED_TOPIC);

                _logger.LogInformation("[RescueMissionService] Successfully confirmed pickup for ReliefOrder {OrderID}, Mission {MissionID}", request.ReliefOrderID, request.RescueMissionID);

                ConfirmPickupResponseDTO response = new ConfirmPickupResponseDTO
                {
                    ReliefOrderID = reliefOrder.ReliefOrderID,
                    RescueMissionID = rescueMission.RescueMissionID,
                    RescueRequestID = rescueMission.RescueRequestID,
                    OrderStatus = reliefOrder.Status,
                    PickedUpTime = pickedUpTime,
                    Message = $"Relief Order {reliefOrder.ReliefOrderID} has been picked up by Team {rescueTeam.TeamName}. Delivery started."
                };

                return response;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "[RescueMissionService - Error] ConfirmPickup failed. Transaction rolled back. OrderID: {OrderID}, MissionID: {MissionID}", request.ReliefOrderID, request.RescueMissionID);
                throw;
            }
        }


        public async Task<PagedResult<RescueMissionListResponseDTO>> GetFilteredMissionAsync(RescueMissionFilterDTO filter)
        {
            _logger.LogInformation("[RescueMissionService] GetFilteredMissions called. Statuses: {Statuses}, TeamID: {TeamID}, Page: {Page}, Size: {Size}",
                filter.Statuses != null ? string.Join(",", filter.Statuses) : "All",
                filter.RescueTeamID, filter.PageNumber, filter.PageSize);

            string cacheKey = BuildMissionFilterCacheKey(filter);

            PagedResult<RescueMissionListResponseDTO>? cached = await _cacheService.GetAsync<PagedResult<RescueMissionListResponseDTO>>(cacheKey);

            if (cached != null)
            {
                _logger.LogInformation("[RescueMissionService - Redis] Cache hit for filter key: {Key}. TotalCount: {Count}", cacheKey, cached.TotalCount);
                return cached;
            }

            _logger.LogInformation("[RescueMissionService - Redis] Cache miss for filter key: {Key}. Querying database.", cacheKey);

            // Lấy bản vẽ query từ base repo để thiết kế bản vẽ query - tức câu lệnh query rồi mới thực hiện truy vấn
            IQueryable<RescueMissionEntity> query = _unitOfWork.RescueMissions.GetQueryable();

            query = query.Where(rm => !rm.IsDeleted);

            if (filter.Statuses != null && filter.Statuses.Count > 0)
            {
                query = query.Where(rm => filter.Statuses.Contains(rm.Status));   
            }

            if (filter.RescueTeamID.HasValue)
            {
                query = query.Where(rm => rm.RescueTeamID == filter.RescueTeamID.Value);
            }

            if (filter.CoordinatorID.HasValue)
            {
                query = query.Where(rm => rm.CoordinatorID == filter.CoordinatorID.Value);
            }


            //lọc theo các mốc thời gian

            // mốc AssignedAt -> mốc thời gian Coordinator đã gắn các nhiệm vụ
            if (filter.AssignedFromDate.HasValue)
            {
                query = query.Where(rm => rm.AssignedAt >= filter.AssignedFromDate.Value);
            }

            if (filter.AssignedToDate.HasValue)
            {
                query = query.Where(rm => rm.AssignedAt <= filter.AssignedToDate.Value);
            }

            // mốc StartTime thời điểm team bấm "chấp nhận" -> InProgress
            // chưa đăng kí tức là start time chưa có trong bảng mission 
            // nếu value start from date ngoài request được truyền vô mà rescue mission chưa có thì bị loại
            // vd: filter.StartFromDate.HasValue != null NHƯNG RescueMission.StartTime.Value == null
            if (filter.StartFromDate.HasValue)
            {
                query = query.Where(rm => rm.StartTime.HasValue && rm.StartTime.Value >= filter.StartFromDate.Value);
            }

            if (filter.StartToDate.HasValue)
            {
                query = query.Where(rm => rm.StartTime.HasValue && rm.StartTime.Value <= filter.StartToDate.Value);
            }

            // mốc EndTime thời điểm team bấm hoàn thành -> Completed
            if (filter.EndFromDate.HasValue)
            {
                query = query.Where(rm => rm.EndTime.HasValue && rm.EndTime >= filter.EndFromDate.Value);
            }

            if (filter.EndToDate.HasValue)
            {
                query = query.Where(rm => rm.EndTime.HasValue && rm.EndTime.Value <= filter.EndToDate.Value);
            }

            int totalCount = await query.CountAsync();

            List<RescueMissionEntity> entities = await query
                                                        .OrderByDescending(rm => rm.AssignedAt)
                                                        .Skip((filter.PageNumber - 1) * filter.PageSize)
                                                        .Take(filter.PageSize)
                                                        .AsNoTracking()
                                                        .ToListAsync();

            List<RescueMissionListResponseDTO> dtos = _mapper.Map<List<RescueMissionListResponseDTO>>(entities);

            //Đóng gói sau đó Cache dữ liệu
            PagedResult<RescueMissionListResponseDTO> result = new()
            {
                Data = dtos,
                TotalCount = totalCount
            };

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            _logger.LogInformation("[RescueMissionService - Redis] Cached filter result. Key: {Key}, DataCount: {Count}, TotalCount: {Total}", cacheKey, dtos.Count, totalCount);

            return result;

        }

        private string BuildMissionFilterCacheKey(RescueMissionFilterDTO filter)
        {
            string statusKey = filter.Statuses != null && filter.Statuses.Count > 0
                ? string.Join(",", filter.Statuses.OrderBy(s => s))
                : "";

            string cacheKey = $"{MISSION_FILTER_PREFIX}s={statusKey}" +
                               $"|t={filter.RescueTeamID}" +
                               $"|af={filter.AssignedFromDate:yyyyMMdd}|at={filter.AssignedToDate:yyyyMMdd}" +
                               $"|sf={filter.StartFromDate:yyyyMMdd}|st={filter.StartToDate:yyyyMMdd}" +
                               $"|ef={filter.EndFromDate:yyyyMMdd}|et={filter.EndToDate:yyyyMMdd}" +
                               $"|rc={filter.CoordinatorID}" +
                               $"|p={filter.PageNumber}|ps={filter.PageSize}";

            return cacheKey;
        }
    }
}
