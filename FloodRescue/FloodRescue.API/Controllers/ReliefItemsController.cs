using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.ReliefItem;
using FloodRescue.Services.DTO.Response.ReliefItem;
using FloodRescue.Services.Interface.ReliefItem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloodRescue.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReliefItemsController : ControllerBase
    {
        private readonly IReliefItemService _service;
        private readonly ILogger<ReliefItemsController> _logger;

        public ReliefItemsController(IReliefItemService service, ILogger<ReliefItemsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<ReliefItemResponseDTO>>>> GetAll()
        {
            _logger.LogInformation("[ReliefItemsController] GET all relief items called.");
            try
            {
                var list = await _service.GetAllAsync();
                _logger.LogInformation("[ReliefItemsController] Returned {Count} relief items.", list.Count);
                return ApiResponse<List<ReliefItemResponseDTO>>.Ok(list, "Get relief items successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ReliefItemsController - Error] GET all relief items failed.");
                return StatusCode(500, ApiResponse<List<ReliefItemResponseDTO>>.Fail("Internal server error", 500));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ReliefItemResponseDTO>>> Get(int id)
        {
            _logger.LogInformation("[ReliefItemsController] GET relief item called. ID: {Id}", id);
            try
            {
                var item = await _service.GetByIdAsync(id);
                if (item == null)
                {
                    _logger.LogWarning("[ReliefItemsController] Relief item ID: {Id} not found.", id);
                    return ApiResponse<ReliefItemResponseDTO>.Fail("Relief item not found", 404);
                }
                _logger.LogInformation("[ReliefItemsController] Relief item ID: {Id} returned.", id);
                return ApiResponse<ReliefItemResponseDTO>.Ok(item, "Get relief item successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ReliefItemsController - Error] GET relief item failed. ID: {Id}", id);
                return StatusCode(500, ApiResponse<ReliefItemResponseDTO>.Fail("Internal server error", 500));
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<ReliefItemResponseDTO>>> Create(CreateReliefItemRequestDTO request)
        {
            _logger.LogInformation("[ReliefItemsController] POST relief item called. Name: {Name}", request.ReliefItemName);
            try
            {
                var result = await _service.CreateAsync(request);
                _logger.LogInformation("[ReliefItemsController] Relief item created. ID: {Id}", result.ReliefItemID);
                return ApiResponse<ReliefItemResponseDTO>.Ok(result, "Create relief item successfully", 201);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ReliefItemsController - Error] POST relief item failed. Name: {Name}", request.ReliefItemName);
                return StatusCode(500, ApiResponse<ReliefItemResponseDTO>.Fail("Internal server error", 500));
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> Update(int id, CreateReliefItemRequestDTO request)
        {
            _logger.LogInformation("[ReliefItemsController] PUT relief item called. ID: {Id}", id);
            try
            {
                var result = await _service.UpdateAsync(id, request);
                if (!result)
                {
                    _logger.LogWarning("[ReliefItemsController] Update relief item failed. ID: {Id}", id);
                    return ApiResponse<bool>.Fail("Update failed", 400);
                }
                _logger.LogInformation("[ReliefItemsController] Relief item ID: {Id} updated.", id);
                return ApiResponse<bool>.Ok(true, "Update relief item successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ReliefItemsController - Error] PUT relief item failed. ID: {Id}", id);
                return StatusCode(500, ApiResponse<bool>.Fail("Internal server error", 500));
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
        {
            _logger.LogInformation("[ReliefItemsController] DELETE relief item called. ID: {Id}", id);
            try
            {
                var result = await _service.DeleteAsync(id);
                if (!result)
                {
                    _logger.LogWarning("[ReliefItemsController] Delete relief item failed. ID: {Id}", id);
                    return ApiResponse<bool>.Fail("Delete failed", 400);
                }
                _logger.LogInformation("[ReliefItemsController] Relief item ID: {Id} deleted.", id);
                return ApiResponse<bool>.Ok(true, "Delete relief item successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ReliefItemsController - Error] DELETE relief item failed. ID: {Id}", id);
                return StatusCode(500, ApiResponse<bool>.Fail("Internal server error", 500));
            }
        }
    }
}
