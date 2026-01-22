using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FloodRescue.Repositories.Context;
using FloodRescue.Repositories.Entites;
using FloodRescue.Services.Interface;
using FloodRescue.Services.Implements;
using FloodRescue.Services.DTO.Request.Warehouse;
using FloodRescue.Services.BusinessModels;

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
        public async Task<ActionResult<IEnumerable<Warehouse>>> GetWarehouses()
        {
            return null;
        }

        // GET: api/Warehouses1/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Warehouse>> GetWarehouse(int id)
        {
            var warehouse = new Warehouse();

            if (warehouse == null)
            {
                return NotFound();
            }

            return warehouse;
        }

        // PUT: api/Warehouses1/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutWarehouse(int id, Warehouse warehouse)
        {
            if (id != warehouse.WarehouseID)
            {
                return BadRequest();
            }

            

            try
            {
                 
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WarehouseExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Warehouses1
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ApiResponse<bool>>> PostWarehouse(CreateWarehouseRequestDTO warehouse)
        {
            var result = await _warehouseService.CreateWarehouseAsync(warehouse);
            if (!result)
            {
                return ApiResponse<bool>.Fail("Create warehouse failed");
            }

            return ApiResponse<bool>.Ok(result,"Create warehouse successfully",200);
        }

        // DELETE: api/Warehouses1/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWarehouse(int id)
        {
            var warehouse = new Warehouse();
            if (warehouse == null)
            {
                return NotFound();
            }



            return NoContent();
        }

        private bool WarehouseExists(int id)
        {
            return false;
        }
    }
}
