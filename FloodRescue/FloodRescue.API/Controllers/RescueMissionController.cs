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
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<DispatchMissionResponseDTO>.Fail("Data is not valid, please check again.", 400));
                } // hàm kiểm tra attribute

                DispatchMissionResponseDTO? result = await _rescueMissionService.DispatchMissionAsync(request);


                if (result == null)
                {
                    return NotFound(ApiResponse<DispatchMissionResponseDTO>.Fail("Can not dispatch mission, please check again", 404));
                }


                return Ok(ApiResponse<DispatchMissionResponseDTO>.Ok(result, "Dispatch Mission Successfully", 200));

            }
            catch(Exception ex)
            {
                _logger.LogError("Something went wrong when call Rescue Mission Service {ex}", ex.Message);
                return ApiResponse<DispatchMissionResponseDTO>.Fail("System had broken", 500);
            }

        }

        public async Task<ActionResult<ApiResponse<RespondMissionResponseDTO>>> DispatchMission([FromBody] RespondMessageRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<RespondMissionResponseDTO>.Fail("Data is not valid, please check again.", 400));
    
                } // hàm kiểm tra attribute

                if (!request.IsAccepted && string.IsNullOrWhiteSpace(request.RejectReason))
                {
                    return BadRequest(ApiResponse<RespondMissionResponseDTO>.Fail("Reject reason is required when the mission is not accepted.", 400));
                }

                RespondMissionResponseDTO? result = await _rescueMissionService.RespondMissionAsync(request);

                if (result == null)
                {
                    return NotFound(ApiResponse<RespondMissionResponseDTO>.Fail("Can not respond mission, mission may not found or not assigned status, please check again", 404));
                }


                string successMessage = request.IsAccepted ? "Mission accepted successfully." : "Mission rejected successfully.";       

                return Ok(ApiResponse<RespondMissionResponseDTO>.Ok(result, successMessage, 200));  

            }
            catch(Exception ex)
            {
                _logger.LogError("Something went wrong when call Rescue Mission Service Response {ex}", ex.Message);
                return ApiResponse<RespondMissionResponseDTO>.Fail("System had broken", 500);

            }
       




    }

}

