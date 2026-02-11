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

        public RescueTeamsController(IRescueTeamService rescueTeamService)
        {
            _rescueTeamService = rescueTeamService;
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<RescueTeamResponseDTO>>> CreateRescueTeam(RescueTeamRequestDTO request)
        {
            RescueTeamResponseDTO result = await _rescueTeamService.CreateRescueTeamAsync(request);
            if (result == null)
            {
                return BadRequest(ApiResponse<RescueTeamResponseDTO>.Fail("Create rescue team failed"));
            }
            return Ok(ApiResponse<RescueTeamResponseDTO>.Ok(result, "Create rescue team successfully", 201));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<RescueTeamResponseDTO>>> UpdateRescueTeam(Guid id, RescueTeamRequestDTO request)
        {
            RescueTeamResponseDTO? result = await _rescueTeamService.UpdateRescueTeamAsync(id, request);
            if (result == null)
            {
                return NotFound(ApiResponse<RescueTeamResponseDTO>.Fail("Rescue team not found or update failed", 404));
            }
            return Ok(ApiResponse<RescueTeamResponseDTO>.Ok(result, "Update rescue team successfully", 200));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<RescueTeamResponseDTO>>> GetRescueTeamById(Guid id)
        {
            RescueTeamResponseDTO? result = await _rescueTeamService.GetRescueTeamByIdAsync(id);
            if (result == null)
            {
                return NotFound(ApiResponse<RescueTeamResponseDTO>.Fail("Rescue team not found", 404));
            }
            return Ok(ApiResponse<RescueTeamResponseDTO>.Ok(result, "Get rescue team successfully", 200));
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<RescueTeamResponseDTO>>>> GetAllRescueTeams()
        {
            List<RescueTeamResponseDTO> result = await _rescueTeamService.GetAllRescueTeamsAsync();
            return Ok(ApiResponse<List<RescueTeamResponseDTO>>.Ok(result, "Get all rescue teams successfully", 200));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteRescueTeam(Guid id)
        {
            bool result = await _rescueTeamService.DeleteRescueTeamAsync(id);
            if (!result)
            {
                return NotFound(ApiResponse<bool>.Fail("Rescue team not found or already deleted", 404));
            }
            return Ok(ApiResponse<bool>.Ok(true, "Delete rescue team successfully", 200));
        }

    }
}
