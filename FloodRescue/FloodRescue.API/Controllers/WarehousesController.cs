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

        public WarehousesController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        // GET: api/Warehouses1
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<ShowWareHouseResponseDTO>>>> GetWarehouses()
        {
            List<ShowWareHouseResponseDTO>? result = await _warehouseService.GetAllWarehousesAsync();
            if (result != null)
            {
                return ApiResponse<List<ShowWareHouseResponseDTO>>.Ok(result, "Get all warehouses successfully", 200);
            }
            return ApiResponse<List<ShowWareHouseResponseDTO>>.Fail("Warehouse not found");
        }

        // GET: api/Warehouses1/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ShowWareHouseResponseDTO>>> GetWarehouse(int id)
        {
            ShowWareHouseResponseDTO? result = await _warehouseService.SearchWarehouseAsync(id);
            if (result == null)
            {
                return ApiResponse<ShowWareHouseResponseDTO>.Fail("Warehouse not found");
            }
            return ApiResponse<ShowWareHouseResponseDTO>.Ok(result, "Get warehouse successfully", 200);
        }

        // PUT: api/Warehouses1/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<UpdateWarehouseResponseDTO>>> PutWarehouse(int id,UpdateWarehouseRequestDTO warehouse)
        {
            UpdateWarehouseResponseDTO? result = await _warehouseService.UpdateWarehouseAsync(id, warehouse);

            if (result == null)
            {
                return NotFound(ApiResponse<UpdateWarehouseResponseDTO>.Fail("Warehouse not found or update failed", 404));
            }
            return Ok(ApiResponse<UpdateWarehouseResponseDTO>.Ok(result, "Update warehouse successfully", 200));
        }

        // POST: api/Warehouses1
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ApiResponse<CreateWarehouseResponseDTO>>> PostWarehouse(CreateWarehouseRequestDTO warehouse)
        {
            CreateWarehouseResponseDTO result = await _warehouseService.CreateWarehouseAsync(warehouse);
            if (result ==  null)
            {
                return ApiResponse<CreateWarehouseResponseDTO>.Fail("Create warehouse failed");
            }

            return ApiResponse<CreateWarehouseResponseDTO>.Ok(result, "Create warehouse successfully", 200);
        }

        // DELETE: api/Warehouses1/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteWarehouse(int id)
        {
            bool result = await _warehouseService.DeleteWarehouseAsync(id);
            if (!result)
            {
                return ApiResponse<bool>.Fail("Delete failed");
            }

            return ApiResponse<bool>.Ok(true,"Delete warehouse successfully", 200);
        }

    }
}
