using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.IncidentReportRequest;using FloodRescue.Services.DTO.Request.RescueMissionRequest;
using FloodRescue.Services.DTO.Response.IncidentResponse;
using FloodRescue.Services.DTO.Response.RescueMissionResponse;
using FloodRescue.Services.Interface.IncidentReport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        /// Coordinator xử lý sự cố: đóng incident, hủy mission, giải phóng team, đưa request về Processing
        /// PUT /api/incidentreports/resolve
        /// </summary>
        [HttpPut("resolve")]
        [Authorize(Roles = "Rescue Coordinator")]
        public async Task<ActionResult<ApiResponse<ResolvedIncidentResponseDTO>>> ResolveIncident([FromBody] ResolvedIncidentRequestDTO request)
        {
            _logger.LogInformation("[IncidentReportsController] PUT resolve incident called. IncidentID: {IncidentID}", request.IncidentReportID);
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("[IncidentReportsController] ResolveIncident validation failed. ModelState invalid.");
                    return BadRequest(ApiResponse<ResolvedIncidentResponseDTO>.Fail("Data is not valid, please check again.", 400));
                }

                // Lấy CurrentUserID từ JWT Token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");

                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid currentUserId))
                {
                    _logger.LogWarning("[IncidentReportsController] Unable to extract UserID from JWT token.");
                    return Unauthorized(ApiResponse<ResolvedIncidentResponseDTO>.Fail("Invalid token. Please login again.", 401));
                }

                _logger.LogInformation("[IncidentReportsController] ResolveIncident by CoordinatorID: {UserID} for IncidentID: {IncidentID}", currentUserId, request.IncidentReportID);

                var (data, errorMessage) = await _incidentReportService.ResolveIncidentAsync(request, currentUserId);

                if (data == null)
                {
                    _logger.LogWarning("[IncidentReportsController] ResolveIncident returned null. IncidentID: {IncidentID}. Error: {Error}", request.IncidentReportID, errorMessage);
                    return BadRequest(ApiResponse<ResolvedIncidentResponseDTO>.Fail(errorMessage ?? "Failed to resolve incident.", 400));
                }

                _logger.LogInformation("[IncidentReportsController] ResolveIncident success. IncidentID: {IncidentID}", request.IncidentReportID);
                return Ok(ApiResponse<ResolvedIncidentResponseDTO>.Ok(data, "Incident resolved successfully.", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[IncidentReportsController - Error] ResolveIncident failed. IncidentID: {IncidentID}", request.IncidentReportID);
                return StatusCode(500, ApiResponse<ResolvedIncidentResponseDTO>.Fail("Internal server error", 500));
            }
        }


         /// <summary>
        /// Đội cứu hộ báo cáo sự cố trong khi đang thực hiện nhiệm vụ
        /// POST /api/incidentreports/report
        /// </summary>
        [HttpPost("report")]
        [Authorize(Roles = "Rescue Team Member")]
        public async Task<ActionResult<ApiResponse<IncidentReportResponseDTO>>> ReportIncident([FromBody] IncidentReportRequestDTO request)
        {
            _logger.LogInformation("[IncidentReportsController] POST report-incident called. MissionID: {MissionID}", request.RescueMissionID);
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("[IncidentReportsController] ReportIncident validation failed. ModelState invalid.");
                    return BadRequest(ApiResponse<IncidentReportResponseDTO>.Fail("Data is not valid, please check again.", 400));
                }

                // Lấy CurrentUserID từ JWT Token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");

                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid currentUserId))
                {
                    _logger.LogWarning("[IncidentReportsController] Unable to extract UserID from JWT token.");
                    return Unauthorized(ApiResponse<IncidentReportResponseDTO>.Fail("Invalid token. Please login again.", 401));
                }

                _logger.LogInformation("[IncidentReportsController] ReportIncident by UserID: {UserID} for MissionID: {MissionID}", currentUserId, request.RescueMissionID);

                var (data, errorMessage) = await _incidentReportService.ReportIncidentAsync(request, currentUserId);

                if (data == null)
                {
                    _logger.LogWarning("[IncidentReportsController] ReportIncident returned null. MissionID: {MissionID}. Error: {Error}", request.RescueMissionID, errorMessage);
                    return BadRequest(ApiResponse<IncidentReportResponseDTO>.Fail(errorMessage ?? "Failed to report incident.", 400));
                }

                _logger.LogInformation("[IncidentReportsController] ReportIncident success. IncidentID: {IncidentID}, MissionID: {MissionID}", data.IncidentReportID, request.RescueMissionID);
                return Ok(ApiResponse<IncidentReportResponseDTO>.Ok(data, "Incident reported successfully.", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[IncidentReportsController - Error] ReportIncident failed. MissionID: {MissionID}", request.RescueMissionID);
                return StatusCode(500, ApiResponse<IncidentReportResponseDTO>.Fail("Internal server error", 500));
            }
        }

        /// <summary>
        /// Lấy danh sách sự cố có lọc theo trạng thái, thời gian và phân trang
        /// GET /api/incidentreports/filter?statuses=Pending&amp;statuses=Resolved&amp;pageNumber=1&amp;pageSize=10
        /// </summary>
        [HttpGet("filter")]
        [Authorize(Roles = "Rescue Coordinator")]
        public async Task<ActionResult<ApiResponse<PagedResult<IncidentListResponseDTO>>>> GetFilteredIncidents([FromQuery] IncidentFilterDTO filter)
        {
            _logger.LogInformation("[IncidentReportsController] GET filter incidents called. Statuses: {Statuses}, Page: {Page}, Size: {Size}",
                filter.Statuses != null ? string.Join(",", filter.Statuses) : "All",
                filter.PageNumber, filter.PageSize);
            try
            {
                PagedResult<IncidentListResponseDTO> result = await _incidentReportService.GetFilteredIncidentsAsync(filter);

                _logger.LogInformation("[IncidentReportsController] GetFilteredIncidents success. DataCount: {Count}, TotalCount: {Total}", result.Data.Count, result.TotalCount);
                return Ok(ApiResponse<PagedResult<IncidentListResponseDTO>>.Ok(result, "Get filtered incidents successfully", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[IncidentReportsController - Error] GET filter incidents failed.");
                return StatusCode(500, ApiResponse<PagedResult<IncidentListResponseDTO>>.Fail("Internal server error", 500));
            }
        }
    }
}
