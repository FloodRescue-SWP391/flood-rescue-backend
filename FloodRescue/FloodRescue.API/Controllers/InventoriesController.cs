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

        /// <summary>
        /// Lấy danh sách tồn kho của một kho cụ thể
        /// GET /api/inventorys?warehouseId=1
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Inventory Manager")]
        public async Task<ActionResult<ApiResponse<List<InventoryItemResponseDTO>>>> GetInventoryByWarehouse(
            [FromQuery] int warehouseId)
        {
            _logger.LogInformation("[InventoriesController] GET inventory called. WarehouseID: {WarehouseID}", warehouseId);

            try
            {
                // Validate input
                if (warehouseId <= 0)
                {
                    _logger.LogWarning("[InventoriesController] Invalid WarehouseID: {WarehouseID}", warehouseId);
                    return BadRequest(ApiResponse<List<InventoryItemResponseDTO>>.Fail("Invalid Warehouse ID", 400));
                }

                // Gọi service
                var (data, errorMessage) = await _inventoryService.GetInventoryByWarehouseAsync(warehouseId);

                // Xử lý kết quả
                if (errorMessage != null)
                {
                    _logger.LogWarning("[InventoriesController] GetInventory failed: {Error}", errorMessage);
                    return NotFound(ApiResponse<List<InventoryItemResponseDTO>>.Fail(errorMessage, 404));
                }

                _logger.LogInformation("[InventoriesController] GetInventory success. WarehouseID: {WarehouseID}, ItemCount: {Count}",
                    warehouseId, data?.Count ?? 0);

                return Ok(ApiResponse<List<InventoryItemResponseDTO>>.Ok(data!, "Inventory list retrieved successfully.", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[InventoriesController - Error] GetInventory failed. WarehouseID: {WarehouseID}", warehouseId);
                return StatusCode(500, ApiResponse<List<InventoryItemResponseDTO>>.Fail("Internal server error", 500));
            }
        }
        /// <summary>
        /// Nhập hàng vào kho - Manager nhập thêm vật tư từ nguồn tài trợ
        /// </summary>
        [HttpPost("receive")]
        [Authorize(Roles = "Inventory Manager")]
        public async Task<ActionResult<ApiResponse<ReceiveInventoryResponseDTO>>> ReceiveInventory(
            [FromBody] ReceiveInventoryRequestDTO request)
        {
            _logger.LogInformation("[InventoriesController] POST receive called. WarehouseID: {WH}, ItemCount: {Count}",
                request.WarehouseID, request.Items?.Count ?? 0);

            try
            {
                // Validate ModelState
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("[InventoriesController] Validation failed. ModelState invalid.");
                    return BadRequest(ApiResponse<ReceiveInventoryResponseDTO>.Fail("Invalid Data", 400));
                }

                // Gọi service
                var (data, errorMessage) = await _inventoryService.ReceiveInventoryAsync(request);

                // Xử lý kết quả
                if (errorMessage != null)
                {
                    _logger.LogWarning("[InventoriesController] ReceiveInventory failed: {Error}", errorMessage);
                    return BadRequest(ApiResponse<ReceiveInventoryResponseDTO>.Fail(errorMessage, 400));
                }

                _logger.LogInformation("[InventoriesController] ReceiveInventory success. WarehouseID: {WH}, ItemsProcessed: {Count}",
                    data!.WarehouseID, data.TotalItemsProcessed);

                return Ok(ApiResponse<ReceiveInventoryResponseDTO>.Ok(data, "Nhập kho thành công", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[InventoriesController - Error] ReceiveInventory failed. WarehouseID: {WH}", request.WarehouseID);
                return StatusCode(500, ApiResponse<ReceiveInventoryResponseDTO>.Fail("Internal server error", 500));
            }
        }
    }
}
