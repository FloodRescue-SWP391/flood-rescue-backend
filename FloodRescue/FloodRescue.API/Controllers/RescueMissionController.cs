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

        [HttpPut("confirm-pickup")]
        public async Task<ActionResult<ApiResponse<ConfirmPickupResponseDTO>>> ConfirmPickup([FromBody] ConfirmPickUpRequestDTO request)
        {
            _logger.LogInformation("[RescueMissionController] PUT confirm-pickup called. MissionID: {MissionID}, OrderID: {OrderID}", request.RescueMissionID, request.ReliefOrderID);
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("[RescueMissionController] ConfirmPickup validation failed. ModelState invalid.");
                    return BadRequest(ApiResponse<ConfirmPickupResponseDTO>.Fail("Data is not valid, please check again.", 400));
                }

                ConfirmPickupResponseDTO? result = await _rescueMissionService.ConfirmPickupAsync(request);

                if (result == null)
                {
                    _logger.LogWarning("[RescueMissionController] ConfirmPickup returned null. MissionID: {MissionID}, OrderID: {OrderID}", request.RescueMissionID, request.ReliefOrderID);
                    return NotFound(ApiResponse<ConfirmPickupResponseDTO>.Fail("Cannot confirm pickup. Relief order may not be in Prepared status, mission may not be in InProgress status, or order does not belong to this mission.", 404));
                }

                _logger.LogInformation("[RescueMissionController] ConfirmPickup success. MissionID: {MissionID}, OrderID: {OrderID}", request.RescueMissionID, request.ReliefOrderID);
                return Ok(ApiResponse<ConfirmPickupResponseDTO>.Ok(result, "Confirm pickup successfully.", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RescueMissionController - Error] ConfirmPickup failed. MissionID: {MissionID}, OrderID: {OrderID}", request.RescueMissionID, request.ReliefOrderID);
                return StatusCode(500, ApiResponse<ConfirmPickupResponseDTO>.Fail("Internal server error", 500));
            }
        }

        [HttpPut("complete")]
        public async Task<ActionResult<ApiResponse<CompleteMissionResponseDTO>>> CompleteMission([FromBody] CompleteMissionRequestDTO request)
        {
            _logger.LogInformation("[RescueMissionController] PUT complete called. MissionID: {MissionID}", request.RescueMissionID);
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("[RescueMissionController] CompleteMission validation failed. ModelState invalid.");
                    return BadRequest(ApiResponse<CompleteMissionResponseDTO>.Fail("Data is not valid, please check again.", 400));
                }

                CompleteMissionResponseDTO? result = await _rescueMissionService.CompleteMissionAsync(request);

                if (result == null)
                {
                    _logger.LogWarning("[RescueMissionController] CompleteMission returned null. MissionID: {MissionID}", request.RescueMissionID);
                    return NotFound(ApiResponse<CompleteMissionResponseDTO>.Fail("Cannot complete mission. Mission may not be found or not in InProgress status, please check again.", 404));
                }

                _logger.LogInformation("[RescueMissionController] CompleteMission success. MissionID: {MissionID}", request.RescueMissionID);
                return Ok(ApiResponse<CompleteMissionResponseDTO>.Ok(result, "Mission completed successfully.", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RescueMissionController - Error] CompleteMission failed. MissionID: {MissionID}", request.RescueMissionID);
                return StatusCode(500, ApiResponse<CompleteMissionResponseDTO>.Fail("Internal server error", 500));
            }
        }
    }
}

