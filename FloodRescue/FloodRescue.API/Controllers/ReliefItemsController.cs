using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.ReliefItem;
using FloodRescue.Services.DTO.Response.ReliefItem;
using FloodRescue.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloodRescue.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReliefItemsController : ControllerBase
    {
        private readonly IReliefItemService _service;

        public ReliefItemsController(IReliefItemService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<ReliefItemResponseDTO>>>> GetAll()
        {
            var list = await _service.GetAllAsync();
            return ApiResponse<List<ReliefItemResponseDTO>>.Ok(list, "Get relief items successfully", 200);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ReliefItemResponseDTO>>> Get(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return ApiResponse<ReliefItemResponseDTO>.Fail("Relief item not found", 404);
            return ApiResponse<ReliefItemResponseDTO>.Ok(item, "Get relief item successfully", 200);
        }

        [HttpPost]
        [Authorize(Roles = "AD,IM")]
        public async Task<ActionResult<ApiResponse<ReliefItemResponseDTO>>> Create(CreateReliefItemRequestDTO request)
        {
            var result = await _service.CreateAsync(request);
            return ApiResponse<ReliefItemResponseDTO>.Ok(result, "Create relief item successfully", 201);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "AD,IM")]
        public async Task<ActionResult<ApiResponse<bool>>> Update(int id, CreateReliefItemRequestDTO request)
        {
            var result = await _service.UpdateAsync(id, request);
            if (!result) return ApiResponse<bool>.Fail("Update failed", 400);
            return ApiResponse<bool>.Ok(true, "Update relief item successfully", 200);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "AD,IM")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result) return ApiResponse<bool>.Fail("Delete failed", 400);
            return ApiResponse<bool>.Ok(true, "Delete relief item successfully", 200);
        }
    }
}
