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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IncidentReportEntity = FloodRescue.Repositories.Entites.IncidentReport;
using RescueMissionEntity = FloodRescue.Repositories.Entites.RescueMission;
using RescueTeamEntity = FloodRescue.Repositories.Entites.RescueTeam;
using RescueRequestEntity = FloodRescue.Repositories.Entites.RescueRequest;

namespace FloodRescue.Services.Implements.IncidentReport
{
    public class IncidentReportService : IIncidentReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<IncidentReportService> _logger;
        private readonly ICacheService _cacheService;
        private readonly IKafkaProducerService _kafkaProducer;

        // Cache keys
        private const string INCIDENT_HISTORY_KEY = "incident:history:all";
        private const string PENDING_INCIDENTS_KEY = "incident:pending:all";
        public IncidentReportService(IUnitOfWork unitOfWork, ILogger<IncidentReportService> logger, ICacheService cacheService, IKafkaProducerService kafkaProducer)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _cacheService = cacheService;
            _kafkaProducer = kafkaProducer;
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

                // Xóa cache vì dữ liệu đã thay đổi
                await Task.WhenAll(
                    _cacheService.RemoveAsync(PENDING_INCIDENTS_KEY),
                    _cacheService.RemoveAsync(INCIDENT_HISTORY_KEY)
                );
                _logger.LogInformation("[IncidentReportService - Redis] Cleared cache for pending and history incidents.");

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
    }
}
