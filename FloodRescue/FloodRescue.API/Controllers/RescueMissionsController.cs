using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.RescueMission;
using FloodRescue.Services.DTO.Response.RescueMission;
using FloodRescue.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace FloodRescue.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RescueMissionsController : ControllerBase
    {
        private readonly IRescueMissionService _service;

        public RescueMissionsController(IRescueMissionService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<RescueMissionResponseDTO>>>> GetAll()
        {
            var list = await _service.GetAllAsync();
            return ApiResponse<List<RescueMissionResponseDTO>>.Ok(list, "Get rescue missions successfully", 200);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<RescueMissionResponseDTO>>> Get(Guid id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return ApiResponse<RescueMissionResponseDTO>.Fail("Rescue mission not found", 404);
            return ApiResponse<RescueMissionResponseDTO>.Ok(item, "Get rescue mission successfully", 200);
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<RescueMissionResponseDTO>>> Create(CreateRescueMissionRequestDTO request)
        {
            var result = await _service.CreateAsync(request);
            return ApiResponse<RescueMissionResponseDTO>.Ok(result, "Create rescue mission successfully", 201);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponse<bool>>> Update(Guid id, CreateRescueMissionRequestDTO request)
        {
            var result = await _service.UpdateAsync(id, request);
            if (!result) return ApiResponse<bool>.Fail("Update failed", 400);
            return ApiResponse<bool>.Ok(true, "Update rescue mission successfully", 200);
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result) return ApiResponse<bool>.Fail("Delete failed", 400);
            return ApiResponse<bool>.Ok(true, "Delete rescue mission successfully", 200);
        }
    }
}