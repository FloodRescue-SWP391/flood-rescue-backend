using FloodRescue.Repositories.Interface;
using FloodRescue.Services.DTO.Kafka;
using FloodRescue.Services.DTO.Request.IncidentReportRequest;
using FloodRescue.Services.DTO.Response.IncidentResponse;
using FloodRescue.Services.Implements.Cache;
using FloodRescue.Services.Interface.Cache;
using FloodRescue.Services.Interface.IncidentReport;
using FloodRescue.Services.Interface.Kafka;
using FloodRescue.Services.SharedSetting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IncidentReportEntity = FloodRescue.Repositories.Entites.IncidentReport;
using RescueMissionEntity = FloodRescue.Repositories.Entites.RescueMission;
using RescueTeamEntity = FloodRescue.Repositories.Entites.RescueTeam;
using RescueRequestEntity = FloodRescue.Repositories.Entites.RescueRequest;

using RescueTeamMemberEntity = FloodRescue.Repositories.Entites.RescueTeamMember;

using FloodRescue.Services.DTO.Response.RescueMissionResponse;
using FloodRescue.Services.DTO.Request.RescueMissionRequest;
using FloodRescue.Services.BusinessModels;
using AutoMapper;

namespace FloodRescue.Services.Implements.IncidentReport
{
    public class IncidentReportService : IIncidentReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<IncidentReportService> _logger;
        private readonly ICacheService _cacheService;
        private readonly IKafkaProducerService _kafkaProducer;
        private readonly IMapper _mapper;

        // Cache keys
        private const string INCIDENT_HISTORY_KEY = "incident:history:all";
        private const string PENDING_INCIDENTS_KEY = "incident:pending:all";
        private const string INCIDENT_FILTER_PREFIX = "incident:filter:";
        private const string INCIDENT_DETAIL_KEY_PREFIX = "incident:detail:";

        private const string PENDING_MISSIONS_KEY_PREFIX = "rescuemission:pending:team:";
        private const string MISSION_FILTER_PREFIX = "rescuemission:filter";
        private const string MISSION_DETAIL_KEY_PREFIX = "rescuemission:detail:";
        public IncidentReportService(IUnitOfWork unitOfWork, ILogger<IncidentReportService> logger, ICacheService cacheService, IKafkaProducerService kafkaProducer, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _cacheService = cacheService;
            _kafkaProducer = kafkaProducer;
            _mapper = mapper;
        }

        /// <summary>
        /// Lấy lịch sử sự cố đã xử lý (Resolved)
        /// </summary>

        public async Task<List<IncidentHistoryResponseDTO>> GetIncidentHistoryAsync()
        {
            _logger.LogInformation("[IncidentReportService] GetIncidentHistory called.");

            // 1. Check cache first
            var cached = await _cacheService.GetAsync<List<IncidentHistoryResponseDTO>>(INCIDENT_HISTORY_KEY);
            if (cached != null)
            {
                _logger.LogInformation("[IncidentReportService - Redis] Cache hit for incident history. Count: {Count}", cached.Count);
                return cached;
            }

            _logger.LogInformation("[IncidentReportService - Redis] Cache miss. Querying DB for incident history.");

            // Query với Include: RescueMission, Reported (User), Resolver (User)
            List<IncidentReportEntity> resolvedIncidents = await _unitOfWork.IncidentReports.GetAllAsync(
                filter: ir => ir.Status == IncidentReportSettings.RESOLVED_STATUS,
                includes: new System.Linq.Expressions.Expression<Func<IncidentReportEntity, object>>[]
                {
                    ir => ir.RescueMission!,
                    ir => ir.Reported!,
                    ir => ir.Resolver!
                }
            );

            if (!resolvedIncidents.Any())
            {
                _logger.LogInformation("[IncidentReportService] No resolved incidents found.");
                return new List<IncidentHistoryResponseDTO>();
            }

            // Lấy danh sách RescueMissionIDs để query RescueTeam
            var missionIds = resolvedIncidents
                .Where(ir => ir.RescueMission != null)
                .Select(ir => ir.RescueMissionID)
                .Distinct()
                .ToList();

            // Query RescueMissions với Include RescueTeam
            var missions = await _unitOfWork.RescueMissions.GetAllAsync(
                filter: m => missionIds.Contains(m.RescueMissionID),
                includes: m => m.RescueTeam!
            );

            // Tạo dictionary để lookup TeamName
            var teamNameDict = missions
                .Where(m => m.RescueTeam != null)
                .ToDictionary(
                    m => m.RescueMissionID,
                    m => m.RescueTeam!.TeamName
                );

            // Mapping sang DTO
            List<IncidentHistoryResponseDTO> result = resolvedIncidents.Select(ir => new IncidentHistoryResponseDTO
            {
                IncidentReportID = ir.IncidentReportID,
                RescueMissionID = ir.RescueMissionID,
                TeamName = teamNameDict.GetValueOrDefault(ir.RescueMissionID, "Unknown"),
                ReporterName = ir.Reported?.FullName ?? "Unknown",
                ResolverName = ir.Resolver?.FullName ?? "Unknown",
                Title = ir.Title,
                Description = ir.Description,
                CoordinatorNote = ir.CoordinatorNote,
                Latitude = ir.Latitiude,
                Longitude = ir.Longitude,
                CreatedTime = ir.CreatedTime,
                ResolvedTime = ir.ResolvedTime
            }).ToList();

            await _cacheService.SetAsync(INCIDENT_HISTORY_KEY, result, TimeSpan.FromMinutes(5));
            _logger.LogInformation("[IncidentReportService - Redis] Cached {Count} resolved incidents.", result.Count);
            return result;
        }
        /// <summary>
        /// Lấy danh sách sự cố đang chờ xử lý (Pending)
        /// </summary>
        public async Task<List<PendingIncidentResponseDTO>> GetPendingIncidentsAsync()
        {
            _logger.LogInformation("[IncidentReportService] GetPendingIncidents called.");
            // 1. Check cache first
            var cached = await _cacheService.GetAsync<List<PendingIncidentResponseDTO>>(PENDING_INCIDENTS_KEY);
            if (cached != null)
            {
                _logger.LogInformation("[IncidentReportService - Redis] Cache hit for pending incidents. Count: {Count}", cached.Count);
                return cached;
            }

            _logger.LogInformation("[IncidentReportService - Redis] Cache miss. Querying DB for pending incidents.");
            // Query với Include: RescueMission -> RescueTeam, Reported (User)
            List<IncidentReportEntity> pendingIncidents = await _unitOfWork.IncidentReports.GetAllAsync(
                filter: ir => ir.Status == IncidentReportSettings.PENDING_STATUS,
                includes: new System.Linq.Expressions.Expression<Func<IncidentReportEntity, object>>[]
                {
                    ir => ir.RescueMission!,
                    ir => ir.Reported!
                }
            );

            if (!pendingIncidents.Any())
            {
                _logger.LogInformation("[IncidentReportService] No pending incidents found.");
                return new List<PendingIncidentResponseDTO>();
            }
            // Lấy danh sách RescueMissionIDs để query RescueTeam
            var missionIds = pendingIncidents
                .Where(ir => ir.RescueMission != null)
                .Select(ir => ir.RescueMissionID)
                .Distinct()
                .ToList();

            // Query RescueMissions với Include RescueTeam (để lấy TeamName)
            var missions = await _unitOfWork.RescueMissions.GetAllAsync(
                filter: m => missionIds.Contains(m.RescueMissionID),
                includes: m => m.RescueTeam!
            );

            // Tạo dictionary để lookup TeamName
            var teamNameDict = missions
                .Where(m => m.RescueTeam != null)
                .ToDictionary(
                    m => m.RescueMissionID,
                    m => m.RescueTeam!.TeamName
                );

            // Mapping sang DTO
            List<PendingIncidentResponseDTO> result = pendingIncidents.Select(ir => new PendingIncidentResponseDTO
            {
                IncidentReportID = ir.IncidentReportID,
                RescueMissionID = ir.RescueMissionID,
                TeamName = teamNameDict.GetValueOrDefault(ir.RescueMissionID, "Unknown"),
                ReporterName = ir.Reported?.FullName ?? "Unknown",
                Title = ir.Title,
                Description = ir.Description,
                Latitude = ir.Latitiude,  // Note: typo in entity (Latitiude)
                Longitude = ir.Longitude,
                CreatedTime = ir.CreatedTime
            }).ToList();

            // 4. Cache the result (TTL ngắn vì pending thay đổi thường xuyên)
            await _cacheService.SetAsync(PENDING_INCIDENTS_KEY, result, TimeSpan.FromMinutes(5));
            _logger.LogInformation("[IncidentReportService - Redis] Cached {Count} pending incidents.", result.Count);
            return result;
        }

        public async Task<(ResolvedIncidentResponseDTO? Data, string? ErrorMessage)> ResolveIncidentAsync(ResolvedIncidentRequestDTO request, Guid currentUserId)
        {
            _logger.LogInformation("[IncidentReportService] Starting ResolveIncident with IncidentReportID: {IncidentID}, ResolvedBy: {UserID}", request.IncidentReportID, currentUserId);

            // Lấy IncidentReport từ DB theo ID
            IncidentReportEntity? incidentReport = await _unitOfWork.IncidentReports.GetAsync(
                (IncidentReportEntity ir) => ir.IncidentReportID == request.IncidentReportID);

            if (incidentReport == null || incidentReport.Status != IncidentReportSettings.PENDING_STATUS)
            {
                _logger.LogWarning("[IncidentReportService - Sql Server] IncidentReport with ID: {IncidentID} not found or not in Pending status", request.IncidentReportID);
                return (null, "Incident report not found or not in Pending status.");
            }

            // Lấy RescueMission liên quan kèm RescueTeam và RescueRequest
            RescueMissionEntity? rescueMission = await _unitOfWork.RescueMissions.GetAsync(
                (RescueMissionEntity rm) => rm.RescueMissionID == incidentReport.RescueMissionID && !rm.IsDeleted,
                rm => rm.RescueTeam!,
                rm => rm.RescueRequest!);

            if (rescueMission == null)
            {
                _logger.LogWarning("[IncidentReportService - Sql Server] RescueMission with ID: {MissionID} not found for IncidentReport {IncidentID}", incidentReport.RescueMissionID, request.IncidentReportID);
                return (null, "Related rescue mission not found.");
            }

            RescueTeamEntity rescueTeam = rescueMission.RescueTeam!;
            RescueRequestEntity rescueRequest = rescueMission.RescueRequest!;

            _logger.LogInformation("[IncidentReportService] Found related Mission {MissionID}, Team {TeamName}, Request {RequestID}",
                rescueMission.RescueMissionID, rescueTeam.TeamName, rescueRequest.RescueRequestID);

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                DateTime resolvedTime = DateTime.UtcNow;

                // Update IncidentReport: Status = Resolved, CoordinatorNote, ResolvedBy, ResolvedTime
                incidentReport.Status = IncidentReportSettings.RESOLVED_STATUS;
                incidentReport.CoordinatorNote = request.CoordinatorNote;
                incidentReport.ResolvedBy = currentUserId;
                incidentReport.ResolvedTime = resolvedTime;

                _logger.LogInformation("[IncidentReportService] IncidentReport {IncidentID} set to Resolved at {ResolvedTime}", request.IncidentReportID, resolvedTime);

                // Update RescueMission: Status = Cancelled, EndTime
                rescueMission.Status = RescueMissionSettings.CANCELLED_STATUS;
                rescueMission.EndTime = resolvedTime;

                _logger.LogInformation("[IncidentReportService] RescueMission {MissionID} set to Cancelled", rescueMission.RescueMissionID);

                // Update RescueTeam: CurrentStatus = Available
                rescueTeam.CurrentStatus = RescueTeamSettings.AVAILABLE_STATUS;

                _logger.LogInformation("[IncidentReportService] RescueTeam {TeamID} - {TeamName} set to Available", rescueTeam.RescueTeamID, rescueTeam.TeamName);

                // Update RescueRequest: Status = Processing
                rescueRequest.Status = RescueRequestSettings.PROCESSING_STATUS;

                _logger.LogInformation("[IncidentReportService] RescueRequest {RequestID} set to Processing", rescueRequest.RescueRequestID);

                int saveResult = await _unitOfWork.SaveChangesAsync();

                if (saveResult <= 0)
                {
                    _logger.LogError("[IncidentReportService - Error] SaveChanges returned 0 rows during resolve incident. IncidentID: {IncidentID}", request.IncidentReportID);
                    await _unitOfWork.RollbackTransactionAsync();
                    return (null, "Failed to save resolved incident.");
                }

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("[IncidentReportService] Transaction committed for ResolveIncident. IncidentID: {IncidentID}", request.IncidentReportID);

                /*
                    private const string INCIDENT_HISTORY_KEY = "incident:history:all";
                    private const string PENDING_INCIDENTS_KEY = "incident:pending:all";
                    private const string INCIDENT_FILTER_PREFIX = "incident:filter:";
                    private const string INCIDENT_DETAIL_KEY_PREFIX = "incident:detail:";


                    private const string PENDING_MISSIONS_KEY_PREFIX = "rescuemission:pending:team:";
                    private const string MISSION_FILTER_PREFIX = "rescuemission:filter";
                    private const string MISSION_DETAIL_KEY_PREFIX = "rescuemission:detail:";
                 
                 */

                // Xóa cache vì dữ liệu đã thay đổi
                await Task.WhenAll(
                    _cacheService.RemovePatternAsync($"{INCIDENT_HISTORY_KEY}*"),
                    _cacheService.RemovePatternAsync($"{PENDING_INCIDENTS_KEY}*"),
                    _cacheService.RemovePatternAsync($"{INCIDENT_FILTER_PREFIX}*"),
                    _cacheService.RemovePatternAsync($"{INCIDENT_DETAIL_KEY_PREFIX}*"),


                    _cacheService.RemovePatternAsync($"{PENDING_MISSIONS_KEY_PREFIX}*"),
                    _cacheService.RemovePatternAsync($"{MISSION_FILTER_PREFIX}*"),
                    _cacheService.RemovePatternAsync($"{MISSION_DETAIL_KEY_PREFIX}*")

                );

                _logger.LogInformation("[IncidentReportService - Redis] Cleared cache for rescue mission");
                _logger.LogInformation("[IncidentReportService - Redis] Cleared cache for incidents.");

                // Gửi message qua Kafka
                IncidentResolvedMessage kafkaMessage = new IncidentResolvedMessage
                {
                    IncidentReportID = incidentReport.IncidentReportID,
                    RescueMissionID = rescueMission.RescueMissionID,
                    RescueTeamID = rescueTeam.RescueTeamID,
                    TeamName = rescueTeam.TeamName,
                    RescueRequestID = rescueRequest.RescueRequestID,
                    CoordinatorNote = incidentReport.CoordinatorNote,
                    IncidentStatus = incidentReport.Status,
                    MissionStatus = rescueMission.Status,
                    TeamStatus = rescueTeam.CurrentStatus,
                    RequestStatus = rescueRequest.Status,
                    ResolvedTime = resolvedTime
                };

                await _kafkaProducer.ProduceAsync(
                    topic: KafkaSettings.INCIDENT_RESOLVED_TOPIC,
                    key: incidentReport.IncidentReportID.ToString(),
                    message: kafkaMessage);

                _logger.LogInformation("[IncidentReportService - Kafka Producer] Kafka message sent to topic {Topic} for IncidentID: {IncidentID}", KafkaSettings.INCIDENT_RESOLVED_TOPIC, request.IncidentReportID);

                // Tạo response DTO
                ResolvedIncidentResponseDTO response = new ResolvedIncidentResponseDTO
                {
                    IncidentReportID = incidentReport.IncidentReportID,
                    RescueMissionID = rescueMission.RescueMissionID,
                    IncidentStatus = incidentReport.Status,
                    CoordinatorNote = incidentReport.CoordinatorNote,
                    ResolvedBy = currentUserId,
                    ResolvedTime = resolvedTime,
                    MissionStatus = rescueMission.Status,
                    TeamName = rescueTeam.TeamName,
                    TeamStatus = rescueTeam.CurrentStatus,
                    RequestStatus = rescueRequest.Status,
                    Message = $"Incident {incidentReport.IncidentReportID} resolved. Mission {rescueMission.RescueMissionID} cancelled. Team {rescueTeam.TeamName} is now available. Request returned to Processing for re-dispatch."
                };

                _logger.LogInformation("[IncidentReportService] Successfully resolved incident with ID: {IncidentID}", request.IncidentReportID);

                return (response, null);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "[IncidentReportService - Error] ResolveIncident failed. Transaction rolled back. IncidentID: {IncidentID}", request.IncidentReportID);
                throw;
            }
        }

        public async Task<(IncidentReportResponseDTO? Data, string? ErrorMessage)> ReportIncidentAsync(IncidentReportRequestDTO request, Guid currentUserId)
        {
           _logger.LogInformation("[IncidentReportService] Starting ReportIncident with MissionID: {MissionID}, ReportedBy: {UserID}", request.RescueMissionID, currentUserId);

            // Lấy RescueMission từ DB kèm theo RescueTeam
              RescueMissionEntity? rescueMission = await _unitOfWork.RescueMissions.GetAsync(
                (RescueMissionEntity rm) => rm.RescueMissionID == request.RescueMissionID && !rm.IsDeleted,
                includes: rm => rm.RescueTeam!);

            if(rescueMission == null)
            {
                _logger.LogWarning("[IncidentReportService - Sql Server] RescueMission with ID: {MissionID} not found", request.RescueMissionID);
                return (null, "Rescue mission not found.");
            }

            // Kiểm tra trạng thái Mission phải là InProgress
            if (rescueMission.Status != RescueMissionSettings.INPROGRESS_STATUS)
            {
                _logger.LogWarning("[IncidentReportService] RescueMission {MissionID} is not in InProgress status. Current status: {Status}", request.RescueMissionID, rescueMission.Status);
                return (null, $"Cannot report incident. Mission status must be InProgress, current status is {rescueMission.Status}.");
            }

            // Lấy RescueTeamID từ currentUserId qua bảng RescueTeamMembers
            RescueTeamMemberEntity? teamMember = await _unitOfWork.RescueTeamMembers.GetAsync(
                (RescueTeamMemberEntity m) => m.UserID == currentUserId && !m.IsDeleted);

            if (teamMember == null)
            {
                _logger.LogWarning("[IncidentReportService - Sql Server] User {UserID} is not a member of any rescue team", currentUserId);
                return (null, "You are not a member of any rescue team.");
            }

               // Đối chiếu RescueTeamID với mission
            if (teamMember.RescueTeamID != rescueMission.RescueTeamID)
            {
                _logger.LogWarning("[IncidentReportService] User {UserID} belongs to TeamID: {UserTeamID}, but mission belongs to TeamID: {MissionTeamID}",
                currentUserId, teamMember.RescueTeamID, rescueMission.RescueTeamID);
                return (null, "You do not belong to the rescue team assigned to this mission.");
            }

               RescueTeamEntity rescueTeam = rescueMission.RescueTeam!;

            _logger.LogInformation("[IncidentReportService] Validated: User {UserID} belongs to Team {TeamName} (ID: {TeamID})", currentUserId, rescueTeam.TeamName, rescueTeam.RescueTeamID);

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                DateTime createdTime = DateTime.UtcNow;

                // Tạo IncidentReport
                var incidentReport = new IncidentReportEntity
                {
                    IncidentReportID = Guid.NewGuid(),
                    RescueMissionID = request.RescueMissionID,
                    ReportedID = currentUserId,
                    Title = request.Title,
                    Description = request.Description,
                    Latitiude = request.Latitude,
                    Longitude = request.Longitude,
                    Status = IncidentReportSettings.PENDING_STATUS,
                    CreatedTime = createdTime
                };

                await _unitOfWork.IncidentReports.AddAsync(incidentReport);

                _logger.LogInformation("[IncidentReportService] IncidentReport created with ID: {IncidentID} for MissionID: {MissionID}", incidentReport.IncidentReportID, request.RescueMissionID);

                // Update RescueMission: Status = Incident
                rescueMission.Status = RescueMissionSettings.INCIDENT_STATUS;

                _logger.LogInformation("[IncidentReportService] RescueMission {MissionID} status set to Incident", request.RescueMissionID);

                int saveResult = await _unitOfWork.SaveChangesAsync();

                if (saveResult <= 0)
                {
                    _logger.LogError("[IncidentReportService - Error] SaveChanges returned 0 rows during report incident. MissionID: {MissionID}", request.RescueMissionID);
                    await _unitOfWork.RollbackTransactionAsync();
                    return (null, "Failed to save incident report.");
                }

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("[IncidentReportService] Transaction committed for ReportIncident. MissionID: {MissionID}", request.RescueMissionID);

                // Xóa cache pending incidents vì có incident mới
                await Task.WhenAll(
                     _cacheService.RemovePatternAsync($"{INCIDENT_HISTORY_KEY}*"),
                     _cacheService.RemovePatternAsync($"{PENDING_INCIDENTS_KEY}*"),
                     _cacheService.RemovePatternAsync($"{INCIDENT_FILTER_PREFIX}*"),
                     _cacheService.RemovePatternAsync($"{INCIDENT_DETAIL_KEY_PREFIX}*"),


                     _cacheService.RemovePatternAsync($"{PENDING_MISSIONS_KEY_PREFIX}*"),
                     _cacheService.RemovePatternAsync($"{MISSION_FILTER_PREFIX}*"),
                     _cacheService.RemovePatternAsync($"{MISSION_DETAIL_KEY_PREFIX}*")

                 );


                _logger.LogInformation("[IncidentReportService - Redis] Cleared cache for rescue mission");

                _logger.LogInformation("[IncidentReportService - Redis] Cleared cache for incidents.");

                // Gửi message qua Kafka
                IncidentReportedMessage kafkaMessage = new IncidentReportedMessage
                {
                    IncidentReportID = incidentReport.IncidentReportID,
                    RescueMissionID = rescueMission.RescueMissionID,
                    RescueTeamID = rescueTeam.RescueTeamID,
                    TeamName = rescueTeam.TeamName,
                    ReportedID = currentUserId,
                    Title = incidentReport.Title,
                    Description = incidentReport.Description,
                    Latitude = incidentReport.Latitiude,
                    Longitude = incidentReport.Longitude,
                    IncidentStatus = incidentReport.Status,
                    MissionStatus = rescueMission.Status,
                    CreatedTime = createdTime
                };

                await _kafkaProducer.ProduceAsync(
                    topic: KafkaSettings.INCIDENT_ALERT_TOPIC,
                    key: incidentReport.IncidentReportID.ToString(),
                    message: kafkaMessage);

                _logger.LogInformation("[IncidentReportService - Kafka Producer] Kafka message sent to topic {Topic} for IncidentID: {IncidentID}", KafkaSettings.INCIDENT_ALERT_TOPIC, incidentReport.IncidentReportID);

                // Tạo response DTO
                IncidentReportResponseDTO response = new IncidentReportResponseDTO
                {
                    IncidentReportID = incidentReport.IncidentReportID,
                    RescueMissionID = rescueMission.RescueMissionID,
                    ReportedID = currentUserId,
                    Title = incidentReport.Title,
                    Description = incidentReport.Description,
                    Latitude = incidentReport.Latitiude,
                    Longitude = incidentReport.Longitude,
                    IncidentStatus = incidentReport.Status,
                    MissionStatus = rescueMission.Status,
                    CreatedTime = createdTime,
                    Message = $"Incident reported for mission {rescueMission.RescueMissionID} by Team {rescueTeam.TeamName}. Mission status locked to Incident."
                };

                _logger.LogInformation("[IncidentReportService] Successfully reported incident with ID: {IncidentID} for MissionID: {MissionID}", incidentReport.IncidentReportID, request.RescueMissionID);

                return (response, null);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "[IncidentReportService - Error] ReportIncident failed. Transaction rolled back. MissionID: {MissionID}", request.RescueMissionID);
                throw;
            }


        }

        public async Task<PagedResult<IncidentListResponseDTO>> GetFilteredIncidentsAsync(IncidentFilterDTO filter)
        {
            _logger.LogInformation("[IncidentReportService] GetFilteredIncidents called. Statuses: {Statuses}, Page: {Page}, Size: {Size}",
                filter.Statuses != null ? string.Join(",", filter.Statuses) : "All",
                filter.PageNumber, filter.PageSize);

            // Check cache
            string cacheKey = BuildIncidentFilterCacheKey(filter);

            PagedResult<IncidentListResponseDTO>? cached = await _cacheService.GetAsync<PagedResult<IncidentListResponseDTO>>(cacheKey);

            if (cached != null)
            {
                _logger.LogInformation("[IncidentReportService - Redis] Cache hit for filter key: {Key}. TotalCount: {Count}", cacheKey, cached.TotalCount);
                return cached;
            }

            _logger.LogInformation("[IncidentReportService - Redis] Cache miss for filter key: {Key}. Querying database.", cacheKey);

            // Khởi tạo query bằng GetQueryable - chưa chạy xuống DB
            IQueryable<IncidentReportEntity> query = _unitOfWork.IncidentReports.GetQueryable();

            // Lọc theo mảng Statuses (Multi-select filter)
            if (filter.Statuses != null && filter.Statuses.Count > 0)
            {
                query = query.Where(ir => filter.Statuses.Contains(ir.Status));
            }

            // Lọc theo mốc CreatedTime
            if (filter.CreatedFromDate.HasValue)
            {
                query = query.Where(ir => ir.CreatedTime >= filter.CreatedFromDate.Value);
            }

            if (filter.CreatedToDate.HasValue)
            {
                query = query.Where(ir => ir.CreatedTime <= filter.CreatedToDate.Value);
            }

            // Lọc theo mốc ResolvedTime
            if (filter.ResolvedFromDate.HasValue)
            {
                query = query.Where(ir => ir.ResolvedTime.HasValue && ir.ResolvedTime.Value >= filter.ResolvedFromDate.Value);
            }

            if (filter.ResolvedToDate.HasValue)
            {
                query = query.Where(ir => ir.ResolvedTime.HasValue && ir.ResolvedTime.Value <= filter.ResolvedToDate.Value);
            }

            // Tính tổng số dòng cho FE làm thanh phân trang
            int totalCount = await query.CountAsync();

            _logger.LogInformation("[IncidentReportService - Sql Server] Total {Count} incident(s) matched filter", totalCount);

            // Sắp xếp ưu tiên sự cố mới nhất + Include RescueMission -> RescueTeam, Reported (User) + phân trang
            List<IncidentReportEntity> entities = await query
                .OrderByDescending(ir => ir.CreatedTime)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Include(ir => ir.RescueMission!)
                    .ThenInclude(rm => rm.RescueTeam!)
                .Include(ir => ir.Reported!)
                .AsNoTracking()
                .ToListAsync();

            _logger.LogInformation("[IncidentReportService - Sql Server] Retrieved {Count} incident(s) for current page", entities.Count);

            // Mapping sang DTO
            List<IncidentListResponseDTO> dtos = entities.Select(ir => new IncidentListResponseDTO
            {
                IncidentReportID = ir.IncidentReportID,
                RescueMissionID = ir.RescueMissionID,
                TeamName = ir.RescueMission?.RescueTeam?.TeamName ?? "Unknown",
                ReporterName = ir.Reported?.FullName ?? "Unknown",
                Title = ir.Title,
                Status = ir.Status,
                CreatedTime = ir.CreatedTime
            }).ToList();

            // Đóng gói và cache
            PagedResult<IncidentListResponseDTO> result = new()
            {
                Data = dtos,
                TotalCount = totalCount
            };

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            _logger.LogInformation("[IncidentReportService - Redis] Cached filter result. Key: {Key}, DataCount: {Count}, TotalCount: {Total}", cacheKey, dtos.Count, totalCount);

            return result;
        }

        private string BuildIncidentFilterCacheKey(IncidentFilterDTO filter)
        {
            string statusKey = filter.Statuses != null && filter.Statuses.Count > 0
                ? string.Join(",", filter.Statuses.OrderBy(s => s))
                : "";

            return $"{INCIDENT_FILTER_PREFIX}s={statusKey}" +
                   $"|cf={filter.CreatedFromDate:yyyyMMdd}|ct={filter.CreatedToDate:yyyyMMdd}" +
                   $"|rf={filter.ResolvedFromDate:yyyyMMdd}|rt={filter.ResolvedToDate:yyyyMMdd}" +
                   $"|p={filter.PageNumber}|ps={filter.PageSize}";
        }
        /// <summary>
        /// Lấy chi tiết một sự cố theo ID
        /// </summary>
        public async Task<IncidentDetailResponseDTO?> GetIncidentDetailByIdAsync(Guid incidentReportId)
        {
            _logger.LogInformation("[IncidentReportService] GetIncidentDetailById called with ID: {Id}", incidentReportId);

            // 1. Check cache first
            string cacheKey = $"{INCIDENT_DETAIL_KEY_PREFIX}{incidentReportId}";
            var cached = await _cacheService.GetAsync<IncidentDetailResponseDTO>(cacheKey);

            if (cached != null)
            {
                _logger.LogInformation("[IncidentReportService - Redis] Cache hit for incident detail. ID: {Id}", incidentReportId);
                return cached;
            }

            _logger.LogInformation("[IncidentReportService - Redis] Cache miss for incident detail. ID: {Id}", incidentReportId);

            // 2. Query IncidentReport với Include: RescueMission, Reported (User), Resolver (User)
            IncidentReportEntity? incident = await _unitOfWork.IncidentReports.GetAsync(
                filter: ir => ir.IncidentReportID == incidentReportId,
                includes: new System.Linq.Expressions.Expression<Func<IncidentReportEntity, object>>[]
                {
            ir => ir.RescueMission!,
            ir => ir.Reported!,
            ir => ir.Resolver!
                }
            );

            // 3. Nếu không tìm thấy, trả về null (Controller sẽ trả 404)
            if (incident == null)
            {
                _logger.LogWarning("[IncidentReportService] Incident not found with ID: {Id}", incidentReportId);
                return null;
            }

            // 4. Lấy TeamName từ RescueMission -> RescueTeam
            string teamName = "Unknown";
            if (incident.RescueMission != null)
            {
                var mission = await _unitOfWork.RescueMissions.GetAsync(
                    filter: m => m.RescueMissionID == incident.RescueMissionID,
                    includes: m => m.RescueTeam!
                );

                if (mission?.RescueTeam != null)
                {
                    teamName = mission.RescueTeam.TeamName;
                }
            }

            // 5. Mapping sang DTO bằng AutoMapper
            IncidentDetailResponseDTO result = _mapper.Map<IncidentDetailResponseDTO>(incident);
            result.TeamName = teamName;

            // 6. Cache the result
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            _logger.LogInformation("[IncidentReportService - Redis] Cached incident detail for ID: {Id}", incidentReportId);

            _logger.LogInformation("[IncidentReportService] GetIncidentDetailById success for ID: {Id}", incidentReportId);
            return result;
        }
    }
}
