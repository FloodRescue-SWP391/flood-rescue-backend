using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.RescueRequestRequest;
using FloodRescue.Services.DTO.Response.RescueRequestResponse;
using FloodRescue.Services.DTO.Response.Warehouse;
using FloodRescue.Services.Implements;
using FloodRescue.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FloodRescue.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RescueRequestController : ControllerBase
    {
        private readonly IRescueRequestService _rescueRequestService;

        public RescueRequestController(IRescueRequestService rescueRequestService)
        {
            _rescueRequestService = rescueRequestService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<RescueRequestResponseDTO>>>> GetAll()
        {
            List<RescueRequestResponseDTO> result = await _rescueRequestService.GetAllRescueRequestsAsync();
            if (result == null)
            {
                return ApiResponse<List<RescueRequestResponseDTO>>.Fail("No Rescue Requests Found", 404);
            }
            return ApiResponse<List<RescueRequestResponseDTO>>.Ok(result, "Get All Rescue Requests Successfully", 200);
        }

        [HttpGet("{id:Guid}")]
        public async Task<ActionResult<ApiResponse<RescueRequestResponseDTO>>> GetById(Guid id)
        {
            RescueRequestResponseDTO? result = await _rescueRequestService.GetRescueRequestByIdAsync(id);
            if (result == null)
            {
                return ApiResponse<RescueRequestResponseDTO>.Fail("Rescue Request Not Found", 404);
            }
            return ApiResponse<RescueRequestResponseDTO>.Ok(result, "Get Rescue Request Successfully", 200);
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<RescueRequestResponseDTO>>> Create([FromBody] RescueRequestRequestDTO request)
        {
            var (data, errorMessage) = await _rescueRequestService.CreateRescueRequestAsync(request);
            if (errorMessage != null)
            {
                return ApiResponse<RescueRequestResponseDTO>.Fail(errorMessage, 400);
            }
            return ApiResponse<RescueRequestResponseDTO>.Ok(data!, "Rescue Request Created Successfully", 201);

        }

        [HttpDelete("{id:Guid}")]
        public async Task<ActionResult<ApiResponse<RescueRequestResponseDTO>>> Delete(Guid id)
        {
            bool isDeleted = await _rescueRequestService.DeleteRescueRequestAsync(id);
            if (!isDeleted)
            {
                return ApiResponse<RescueRequestResponseDTO>.Fail("Rescue Request Not Found", 404);
            }
            return ApiResponse<RescueRequestResponseDTO>.Ok(null!, "Rescue Request Deleted Successfully", 204);
        }

        [HttpPut("{id:Guid}")]
        public async Task<ActionResult<ApiResponse<RescueRequestResponseDTO>>> Update(Guid id, [FromBody] RescueRequestRequestDTO request)
        {
            var (result, errorMessage) = await _rescueRequestService.UpdateRescueRequestAsync(id, request);
            if (errorMessage != null)
            {
                return ApiResponse<RescueRequestResponseDTO>.Fail(errorMessage, 400);
            }
            return ApiResponse<RescueRequestResponseDTO>.Ok(result!, "Rescue Request Updated Successfully", 200);
        }
    }
}
