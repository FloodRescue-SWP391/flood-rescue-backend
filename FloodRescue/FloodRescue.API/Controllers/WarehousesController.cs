using FloodRescue.Repositories.Context;
using FloodRescue.Repositories.Entites;
using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.Warehouse;
using FloodRescue.Services.DTO.Request.WarehouseRequest;
using FloodRescue.Services.DTO.Response.Warehouse;
using FloodRescue.Services.Implements;
using FloodRescue.Services.Interface.Warehouse;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FloodRescue.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehousesController : ControllerBase
    {
       private readonly IWarehouseService _warehouseService;
       private readonly ILogger<WarehousesController> _logger;

        public WarehousesController(IWarehouseService warehouseService, ILogger<WarehousesController> logger)
        {
            _warehouseService = warehouseService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<ShowWareHouseResponseDTO>>>> GetWarehouses()
        {
            _logger.LogInformation("[WarehousesController] GET all warehouses called.");
            try
            {
                List<ShowWareHouseResponseDTO>? result = await _warehouseService.GetAllWarehousesAsync();
                if (result != null)
                {
                    _logger.LogInformation("[WarehousesController] Returned {Count} warehouses.", result.Count);
                    return ApiResponse<List<ShowWareHouseResponseDTO>>.Ok(result, "Get all warehouses successfully", 200);
                }
                _logger.LogWarning("[WarehousesController] GET all warehouses returned null.");
                return ApiResponse<List<ShowWareHouseResponseDTO>>.Fail("Warehouse not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WarehousesController - Error] GET all warehouses failed.");
                return StatusCode(500, ApiResponse<List<ShowWareHouseResponseDTO>>.Fail("Internal server error", 500));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ShowWareHouseResponseDTO>>> GetWarehouse(int id)
        {
            _logger.LogInformation("[WarehousesController] GET warehouse called. ID: {Id}", id);
            try
            {
                ShowWareHouseResponseDTO? result = await _warehouseService.SearchWarehouseAsync(id);
                if (result == null)
                {
                    _logger.LogWarning("[WarehousesController] Warehouse ID: {Id} not found.", id);
                    return ApiResponse<ShowWareHouseResponseDTO>.Fail("Warehouse not found");
                }
                _logger.LogInformation("[WarehousesController] Warehouse ID: {Id} returned.", id);
                return ApiResponse<ShowWareHouseResponseDTO>.Ok(result, "Get warehouse successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WarehousesController - Error] GET warehouse failed. ID: {Id}", id);
                return StatusCode(500, ApiResponse<ShowWareHouseResponseDTO>.Fail("Internal server error", 500));
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<UpdateWarehouseResponseDTO>>> PutWarehouse(int id, UpdateWarehouseRequestDTO warehouse)
        {
            _logger.LogInformation("[WarehousesController] PUT warehouse called. ID: {Id}", id);
            try
            {
                UpdateWarehouseResponseDTO? result = await _warehouseService.UpdateWarehouseAsync(id, warehouse);

                if (result == null)
                {
                    _logger.LogWarning("[WarehousesController] Update warehouse failed. ID: {Id}", id);
                    return NotFound(ApiResponse<UpdateWarehouseResponseDTO>.Fail("Warehouse not found or update failed", 404));
                }
                _logger.LogInformation("[WarehousesController] Warehouse ID: {Id} updated.", id);
                return Ok(ApiResponse<UpdateWarehouseResponseDTO>.Ok(result, "Update warehouse successfully", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WarehousesController - Error] PUT warehouse failed. ID: {Id}", id);
                return StatusCode(500, ApiResponse<UpdateWarehouseResponseDTO>.Fail("Internal server error", 500));
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<CreateWarehouseResponseDTO>>> PostWarehouse(CreateWarehouseRequestDTO warehouse)
        {
            _logger.LogInformation("[WarehousesController] POST warehouse called. Name: {Name}", warehouse.Name);
            try
            {
                CreateWarehouseResponseDTO result = await _warehouseService.CreateWarehouseAsync(warehouse);
                if (result == null)
                {
                    _logger.LogWarning("[WarehousesController] Create warehouse failed.");
                    return ApiResponse<CreateWarehouseResponseDTO>.Fail("Create warehouse failed");
                }
                _logger.LogInformation("[WarehousesController] Warehouse created. Name: {Name}", result.Name);
                return ApiResponse<CreateWarehouseResponseDTO>.Ok(result, "Create warehouse successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WarehousesController - Error] POST warehouse failed. Name: {Name}", warehouse.Name);
                return StatusCode(500, ApiResponse<CreateWarehouseResponseDTO>.Fail("Internal server error", 500));
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteWarehouse(int id)
        {
            _logger.LogInformation("[WarehousesController] DELETE warehouse called. ID: {Id}", id);
            try
            {
                bool result = await _warehouseService.DeleteWarehouseAsync(id);
                if (!result)
                {
                    _logger.LogWarning("[WarehousesController] Delete warehouse failed. ID: {Id}", id);
                    return ApiResponse<bool>.Fail("Delete failed");
                }
                _logger.LogInformation("[WarehousesController] Warehouse ID: {Id} deleted.", id);
                return ApiResponse<bool>.Ok(true, "Delete warehouse successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WarehousesController - Error] DELETE warehouse failed. ID: {Id}", id);
                return StatusCode(500, ApiResponse<bool>.Fail("Internal server error", 500));
            }
        }

    }
}
