using FloodRescue.Repositories.Interface;
using FloodRescue.Services.DTO.Response.IncidentResponse;
using FloodRescue.Services.Interface.IncidentReport;
using FloodRescue.Services.SharedSetting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IncidentReportEntity = FloodRescue.Repositories.Entites.IncidentReport;

namespace FloodRescue.Services.Implements.IncidentReport
{
    public class IncidentReportService : IIncidentReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<IncidentReportService> _logger;

        public IncidentReportService(IUnitOfWork unitOfWork, ILogger<IncidentReportService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Lấy lịch sử sự cố đã xử lý (Resolved)
        /// </summary>

        public async Task<List<IncidentHistoryResponseDTO>> GetIncidentHistoryAsync()
        {
            _logger.LogInformation("[IncidentReportService] GetIncidentHistory called.");

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

            _logger.LogInformation("[IncidentReportService] Found {Count} resolved incidents.", result.Count);
            return result;
        }
        /// <summary>
        /// Lấy danh sách sự cố đang chờ xử lý (Pending)
        /// </summary>
        public async Task<List<PendingIncidentResponseDTO>> GetPendingIncidentsAsync()
        {
            _logger.LogInformation("[IncidentReportService] GetPendingIncidents called.");
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

            _logger.LogInformation("[IncidentReportService] Found {Count} pending incidents.", result.Count);
            return result;
        }


    }
}
