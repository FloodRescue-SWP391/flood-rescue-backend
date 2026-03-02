using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.InventoryRequest;
using FloodRescue.Services.DTO.Response.InventoryResponse;
using FloodRescue.Services.Interface.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloodRescue.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoriesController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;
        private readonly ILogger<InventoriesController> _logger;

        public InventoriesController(IInventoryService inventoryService, ILogger<InventoriesController> logger)
        {
            _inventoryService = inventoryService;
            _logger = logger;
        }

        [HttpPut("adjust")]
        [Authorize(Roles = "Inventory Manager")]
        public async Task<ActionResult<ApiResponse<AdjustInventoryResponseDTO>>> AdjustInventory([FromBody] AdjustInventoryRequestDTO request)
        {
            _logger.LogInformation("[InventoriesController] PUT adjust-inventory called. WarehouseID: {WarehouseID}, Items count: {Count}", request.WarehouseID, request.Items.Count);
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("[InventoriesController] AdjustInventory validation failed. ModelState invalid.");
                    return BadRequest(ApiResponse<AdjustInventoryResponseDTO>.Fail("Data is not valid, please check again.", 400));
                }

                var (data, errorMessage) = await _inventoryService.AdjustInventoryAsync(request);

                if (data == null)
                {
                    _logger.LogWarning("[InventoriesController] AdjustInventory returned null. WarehouseID: {WarehouseID}. Error: {Error}", request.WarehouseID, errorMessage);
                    return BadRequest(ApiResponse<AdjustInventoryResponseDTO>.Fail(errorMessage ?? "Failed to adjust inventory.", 400));
                }

                _logger.LogInformation("[InventoriesController] AdjustInventory success. WarehouseID: {WarehouseID}, Adjusted {Count} item(s)", request.WarehouseID, data.AdjustedItems.Count);
                return Ok(ApiResponse<AdjustInventoryResponseDTO>.Ok(data, "Inventory adjusted successfully.", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[InventoriesController - Error] AdjustInventory failed. WarehouseID: {WarehouseID}", request.WarehouseID);
                return StatusCode(500, ApiResponse<AdjustInventoryResponseDTO>.Fail("Internal server error", 500));
            }
        }
    }
}
