using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.RescueMissionRequest;
using FloodRescue.Services.DTO.Response.RescueMissionResponse;
using FloodRescue.Services.Interface.RescueMission;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace FloodRescue.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RescueMissionController : ControllerBase
    {
        private readonly IRescueMissionService _rescueMissionService;
        public RescueMissionController(IRescueMissionService rescueMissionService)
        {
            _rescueMissionService = rescueMissionService;
        }

        public async Task<ActionResult<ApiResponse<DispatchMissionResponseDTO>>> DispatchMission([FromBody] DispatchMissionRequestDTO request)
        {
            if (!ModelState.IsValid) {
                return BadRequest(ApiResponse<DispatchMissionResponseDTO>.Fail("Data is not valid, please check again.", 400));
            } // hàm kiểm tra attribute

           DispatchMissionResponseDTO? result = await _rescueMissionService.DispatchMissionAsync(request);


           if(result == null)
            {
                return NotFound(ApiResponse<DispatchMissionResponseDTO>.Fail("Can not dispatch mission, please check again", 404));
            }


            return Ok(ApiResponse<DispatchMissionResponseDTO>.Ok(result, "Dispatch Mission Successfully", 200));

        }





    }

}

