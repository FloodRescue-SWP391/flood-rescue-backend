using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.RescueTeamRequest;
using FloodRescue.Services.DTO.Response.RescueTeamResponse;
using FloodRescue.Services.Interface.RescueTeam;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FloodRescue.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RescueTeamsController : ControllerBase
    {
        private readonly IRescueTeamService _rescueTeamService;
        private readonly ILogger<RescueTeamsController> _logger;

        public RescueTeamsController(IRescueTeamService rescueTeamService, ILogger<RescueTeamsController> logger)
        {
            _rescueTeamService = rescueTeamService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<RescueTeamResponseDTO>>> CreateRescueTeam(RescueTeamRequestDTO request)
        {
            _logger.LogInformation("[RescueTeamsController] POST create rescue team called. TeamName: {TeamName}", request.TeamName);
            try
            {
                RescueTeamResponseDTO result = await _rescueTeamService.CreateRescueTeamAsync(request);
                if (result == null)
                {
                    _logger.LogWarning("[RescueTeamsController] Create rescue team failed.");
                    return BadRequest(ApiResponse<RescueTeamResponseDTO>.Fail("Create rescue team failed"));
                }
                _logger.LogInformation("[RescueTeamsController] Rescue team created. ID: {Id}", result.RescueTeamID);
                return Ok(ApiResponse<RescueTeamResponseDTO>.Ok(result, "Create rescue team successfully", 201));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RescueTeamsController - Error] POST create rescue team failed. TeamName: {TeamName}", request.TeamName);
                return StatusCode(500, ApiResponse<RescueTeamResponseDTO>.Fail("Internal server error", 500));
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<RescueTeamResponseDTO>>> UpdateRescueTeam(Guid id, RescueTeamRequestDTO request)
        {
            _logger.LogInformation("[RescueTeamsController] PUT rescue team called. ID: {Id}", id);
            try
            {
                RescueTeamResponseDTO? result = await _rescueTeamService.UpdateRescueTeamAsync(id, request);
                if (result == null)
                {
                    _logger.LogWarning("[RescueTeamsController] Update rescue team failed. ID: {Id}", id);
                    return NotFound(ApiResponse<RescueTeamResponseDTO>.Fail("Rescue team not found or update failed", 404));
                }
                _logger.LogInformation("[RescueTeamsController] Rescue team ID: {Id} updated.", id);
                return Ok(ApiResponse<RescueTeamResponseDTO>.Ok(result, "Update rescue team successfully", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RescueTeamsController - Error] PUT rescue team failed. ID: {Id}", id);
                return StatusCode(500, ApiResponse<RescueTeamResponseDTO>.Fail("Internal server error", 500));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<RescueTeamResponseDTO>>> GetRescueTeamById(Guid id)
        {
            _logger.LogInformation("[RescueTeamsController] GET rescue team called. ID: {Id}", id);
            try
            {
                RescueTeamResponseDTO? result = await _rescueTeamService.GetRescueTeamByIdAsync(id);
                if (result == null)
                {
                    _logger.LogWarning("[RescueTeamsController] Rescue team ID: {Id} not found.", id);
                    return NotFound(ApiResponse<RescueTeamResponseDTO>.Fail("Rescue team not found", 404));
                }
                _logger.LogInformation("[RescueTeamsController] Rescue team ID: {Id} returned.", id);
                return Ok(ApiResponse<RescueTeamResponseDTO>.Ok(result, "Get rescue team successfully", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RescueTeamsController - Error] GET rescue team failed. ID: {Id}", id);
                return StatusCode(500, ApiResponse<RescueTeamResponseDTO>.Fail("Internal server error", 500));
            }
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<RescueTeamResponseDTO>>>> GetAllRescueTeams()
        {
            _logger.LogInformation("[RescueTeamsController] GET all rescue teams called.");
            try
            {
                List<RescueTeamResponseDTO> result = await _rescueTeamService.GetAllRescueTeamsAsync();
                _logger.LogInformation("[RescueTeamsController] Returned {Count} rescue teams.", result.Count);
                return Ok(ApiResponse<List<RescueTeamResponseDTO>>.Ok(result, "Get all rescue teams successfully", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RescueTeamsController - Error] GET all rescue teams failed.");
                return StatusCode(500, ApiResponse<List<RescueTeamResponseDTO>>.Fail("Internal server error", 500));
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteRescueTeam(Guid id)
        {
            _logger.LogInformation("[RescueTeamsController] DELETE rescue team called. ID: {Id}", id);
            try
            {
                bool result = await _rescueTeamService.DeleteRescueTeamAsync(id);
                if (!result)
                {
                    _logger.LogWarning("[RescueTeamsController] Delete rescue team failed. ID: {Id}", id);
                    return NotFound(ApiResponse<bool>.Fail("Rescue team not found or already deleted", 404));
                }
                _logger.LogInformation("[RescueTeamsController] Rescue team ID: {Id} deleted.", id);
                return Ok(ApiResponse<bool>.Ok(true, "Delete rescue team successfully", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RescueTeamsController - Error] DELETE rescue team failed. ID: {Id}", id);
                return StatusCode(500, ApiResponse<bool>.Fail("Internal server error", 500));
            }
        }

    }
}
