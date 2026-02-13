using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.RescueMissionRequest;
using FloodRescue.Services.DTO.Response.RescueMissionResponse;
using FloodRescue.Services.Interface.RescueMission;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Contracts;
namespace FloodRescue.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RescueMissionController : ControllerBase
    {
        private readonly IRescueMissionService _rescueMissionService;
        private readonly ILogger<RescueMissionController> _logger;
        public RescueMissionController(IRescueMissionService rescueMissionService, ILogger<RescueMissionController> logger)
        {
            _rescueMissionService = rescueMissionService;
            _logger = logger;
        }

        [HttpPost("dispatch")]
        public async Task<ActionResult<ApiResponse<DispatchMissionResponseDTO>>> DispatchMission([FromBody] DispatchMissionRequestDTO request)
        {
            _logger.LogInformation("[RescueMissionController] POST dispatch called. RequestID: {RequestID}, TeamID: {TeamID}", request.RescueRequestID, request.RescueTeamID);
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("[RescueMissionController] Dispatch validation failed. ModelState invalid.");
                    return BadRequest(ApiResponse<DispatchMissionResponseDTO>.Fail("Data is not valid, please check again.", 400));
                }

                DispatchMissionResponseDTO? result = await _rescueMissionService.DispatchMissionAsync(request);

                if (result == null)
                {
                    _logger.LogWarning("[RescueMissionController] Dispatch returned null. RequestID: {RequestID}, TeamID: {TeamID}", request.RescueRequestID, request.RescueTeamID);
                    return NotFound(ApiResponse<DispatchMissionResponseDTO>.Fail("Can not dispatch mission, please check again", 404));
                }

                _logger.LogInformation("[RescueMissionController] Dispatch success. MissionID: {MissionID}", result.RescueMissionID);
                return Ok(ApiResponse<DispatchMissionResponseDTO>.Ok(result, "Dispatch Mission Successfully", 200));

            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "[RescueMissionController - Error] Dispatch failed. RequestID: {RequestID}, TeamID: {TeamID}", request.RescueRequestID, request.RescueTeamID);
                return StatusCode(500, ApiResponse<DispatchMissionResponseDTO>.Fail("Internal server error", 500));
            }

        }

        [HttpPost("respond")]
        public async Task<ActionResult<ApiResponse<RespondMissionResponseDTO>>> RespondMission([FromBody] RespondMessageRequestDTO request)
        {
            _logger.LogInformation("[RescueMissionController] POST respond called. MissionID: {MissionID}, IsAccepted: {IsAccepted}", request.RescueMissionID, request.IsAccepted);
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("[RescueMissionController] Respond validation failed. ModelState invalid.");
                    return BadRequest(ApiResponse<RespondMissionResponseDTO>.Fail("Data is not valid, please check again.", 400));
    
                }

                if (!request.IsAccepted && string.IsNullOrWhiteSpace(request.RejectReason))
                {
                    _logger.LogWarning("[RescueMissionController] Reject reason missing for MissionID: {MissionID}", request.RescueMissionID);
                    return BadRequest(ApiResponse<RespondMissionResponseDTO>.Fail("Reject reason is required when the mission is not accepted.", 400));
                }

                RespondMissionResponseDTO? result = await _rescueMissionService.RespondMissionAsync(request);

                if (result == null)
                {
                    _logger.LogWarning("[RescueMissionController] Respond returned null. MissionID: {MissionID}", request.RescueMissionID);
                    return NotFound(ApiResponse<RespondMissionResponseDTO>.Fail("Can not respond mission, mission may not found or not assigned status, please check again", 404));
                }

                string successMessage = request.IsAccepted ? "Mission accepted successfully." : "Mission rejected successfully.";       
                _logger.LogInformation("[RescueMissionController] Respond success. MissionID: {MissionID}, Result: {Result}", request.RescueMissionID, successMessage);
                return Ok(ApiResponse<RespondMissionResponseDTO>.Ok(result, successMessage, 200));  

            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "[RescueMissionController - Error] Respond failed. MissionID: {MissionID}", request.RescueMissionID);
                return StatusCode(500, ApiResponse<RespondMissionResponseDTO>.Fail("Internal server error", 500));

            }
        }
    }
}

