using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Response.IncidentResponse;
using FloodRescue.Services.Interface.IncidentReport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FloodRescue.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IncidentReportsController : ControllerBase
    {
        private readonly IIncidentReportService _incidentReportService;
        private readonly ILogger<IncidentReportsController> _logger;

        public IncidentReportsController(IIncidentReportService incidentReportService, ILogger<IncidentReportsController> logger)
        {
            _incidentReportService = incidentReportService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách sự cố đang chờ xử lý (Pending) - Dashboard cho Coordinator
        /// GET /api/incidentreports/pending
        /// </summary>
        [HttpGet("pending")]
        [Authorize(Roles = "Rescue Coordinator")]
        public async Task<ActionResult<ApiResponse<List<PendingIncidentResponseDTO>>>> GetPendingIncidents()
        {
            _logger.LogInformation("[IncidentReportsController] GET pending incidents called.");

            try
            {
                List<PendingIncidentResponseDTO> result = await _incidentReportService.GetPendingIncidentsAsync();

                _logger.LogInformation("[IncidentReportsController] GetPendingIncidents success. Found {Count} incidents.", result.Count);
                return Ok(ApiResponse<List<PendingIncidentResponseDTO>>.Ok(result, "Get pending incidents successfully", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[IncidentReportsController - Error] GetPendingIncidents failed.");
                return StatusCode(500, ApiResponse<List<PendingIncidentResponseDTO>>.Fail("Internal server error", 500));
            }
        }

        /// <summary>
        /// Lấy lịch sử sự cố đã xử lý (Resolved) - Cho báo cáo KPI
        /// GET /api/incidentreports/history
        /// </summary>
        [HttpGet("history")]
        [Authorize(Roles = "Rescue Coordinator")]
        public async Task<ActionResult<ApiResponse<List<IncidentHistoryResponseDTO>>>> GetIncidentHistory()
        {
            _logger.LogInformation("[IncidentReportsController] GET incident history called.");

            try
            {
                List<IncidentHistoryResponseDTO> result = await _incidentReportService.GetIncidentHistoryAsync();

                _logger.LogInformation("[IncidentReportsController] GetIncidentHistory success. Found {Count} incidents.", result.Count);
                return Ok(ApiResponse<List<IncidentHistoryResponseDTO>>.Ok(result, "Get incident history successfully", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[IncidentReportsController - Error] GetIncidentHistory failed.");
                return StatusCode(500, ApiResponse<List<IncidentHistoryResponseDTO>>.Fail("Internal server error", 500));
            }
        }

        /// <summary>
        /// Lấy chi tiết một sự cố theo ID - Cho Coordinator/Admin
        /// GET /api/incidentreports/{id}
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Rescue Coordinator,Admin")]
        public async Task<ActionResult<ApiResponse<IncidentDetailResponseDTO>>> GetIncidentDetail(Guid id)
        {
            _logger.LogInformation("[IncidentReportsController] GET incident detail called with ID: {Id}", id);

            try
            {
                IncidentDetailResponseDTO? result = await _incidentReportService.GetIncidentDetailByIdAsync(id);

                if (result == null)
                {
                    _logger.LogWarning("[IncidentReportsController] Incident not found with ID: {Id}", id);
                    return NotFound(ApiResponse<IncidentDetailResponseDTO>.Fail("Incident not found", 404));
                }

                _logger.LogInformation("[IncidentReportsController] GetIncidentDetail success for ID: {Id}", id);
                return Ok(ApiResponse<IncidentDetailResponseDTO>.Ok(result, "Get incident detail successfully", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[IncidentReportsController - Error] GetIncidentDetail failed for ID: {Id}", id);
                return StatusCode(500, ApiResponse<IncidentDetailResponseDTO>.Fail("Internal server error", 500));
            }
        }
    }
}
