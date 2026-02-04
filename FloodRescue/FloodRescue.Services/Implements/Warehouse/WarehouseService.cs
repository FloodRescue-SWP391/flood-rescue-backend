using AutoMapper;
using FloodRescue.Repositories.Entites;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.DTO.Request.Warehouse;
using FloodRescue.Services.DTO.Request.WarehouseRequest;
using FloodRescue.Services.DTO.Response.Warehouse;
using FloodRescue.Services.Interface.Cache;
using FloodRescue.Services.Interface.Warehouse;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WarehouseEntity = FloodRescue.Repositories.Entites.Warehouse;

namespace FloodRescue.Services.Implements.Warehouse
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly ILogger<WarehouseService> _logger; 

        public WarehouseService(IUnitOfWork unitOfWork, IMapper mapper, ICacheService cacheService, ILogger<WarehouseService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
            _logger = logger;

        }

        //lấy tất
        private const string ALL_WAREHOUSES_KEY = "warehouse:all";
        //lấy theo id
        private const string WAREHOUSE_KEY_PREFIX = "warehouse:";

        public async Task<CreateWarehouseResponseDTO> CreateWarehouseAsync(CreateWarehouseRequestDTO request)
        {
            WarehouseEntity warehouse = _mapper.Map<WarehouseEntity>(request);
            await _unitOfWork.Warehouses.AddAsync(warehouse);
            await _unitOfWork.SaveChangesAsync();

            CreateWarehouseResponseDTO responseDTO = _mapper.Map<CreateWarehouseResponseDTO>(warehouse);

            await _cacheService.RemoveAsync(ALL_WAREHOUSES_KEY);    

            return responseDTO;
        }

        public async Task<bool> DeleteWarehouseAsync(int warehouseId)
        {
            WarehouseEntity? warehouse = await _unitOfWork.Warehouses.GetAsync(w => w.WarehouseID == warehouseId);
            int result = 0;
            if (warehouse != null)
            {
                if (warehouse.IsDeleted == false)
                {
                    warehouse.IsDeleted = true;
                    result = await _unitOfWork.SaveChangesAsync();
                }
            }
            return result > 0;
        }
        public async Task<ShowWareHouseResponseDTO?> SearchWarehouseAsync(int id)
        {
            ShowWareHouseResponseDTO? responseDTO = null;
            WarehouseEntity? warehouse = await _unitOfWork.Warehouses.GetAsync(
                w => w.WarehouseID == id && !w.IsDeleted,
                w => w.Manager!
            );
            if (warehouse != null)
            {
                responseDTO = _mapper.Map<ShowWareHouseResponseDTO>(warehouse);
            }
            return responseDTO;
        }

        public async Task<List<ShowWareHouseResponseDTO>> GetAllWarehousesAsync()
        {
            _logger.LogInformation("Getting all Warehouse");

            var cached = await _cacheService.GetAsync<List<ShowWareHouseResponseDTO>>(ALL_WAREHOUSES_KEY);

            if(cached != null)
            {

                return cached;
            }

            List<WarehouseEntity> warehouse = await _unitOfWork.Warehouses.GetAllAsync(
                w => !w.IsDeleted,
                w => w.Manager!
            );

            var result = _mapper.Map<List<ShowWareHouseResponseDTO>>(warehouse);

            _logger.LogInformation("Retrieved {Count} warehouses from database", result.Count);

            await _cacheService.SetAsync(ALL_WAREHOUSES_KEY, result, TimeSpan.FromMinutes(5));

            _logger.LogInformation("Cached {Count} warehouses", result.Count);

            return result;
        }

        public async Task<UpdateWarehouseResponseDTO> UpdateWarehouseAsync(int id, UpdateWarehouseRequestDTO request)
        {
            WarehouseEntity? _warehouse = await _unitOfWork.Warehouses.GetAsync(w => w.WarehouseID == id);

            if (_warehouse == null)
            {
                return null;
            }

            WarehouseEntity newWarehouse = _mapper.Map<WarehouseEntity>(request);
            //_warehouse.Name = request.Name;
            //_warehouse.Address = request.Address;
            //_warehouse.LocationLong = request.LocationLong;
            //_warehouse.LocationLat = request.LocationLat;
            //_warehouse.IsDeleted = request.IsDeleted;

            int result = await _unitOfWork.SaveChangesAsync();

            if (result > 0)
            {
                await _cacheService.RemoveAsync(ALL_WAREHOUSES_KEY);
                await _cacheService.RemoveAsync($"{WAREHOUSE_KEY_PREFIX}{id}"); 

                return _mapper.Map<UpdateWarehouseResponseDTO>(newWarehouse);
            }
            return null;
        }
    }
}
