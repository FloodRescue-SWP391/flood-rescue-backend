using AutoMapper;
using Confluent.Kafka;
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
            _logger.LogInformation("Request to delete Warehouse ID: {WarehouseId}", warehouseId);
            WarehouseEntity? warehouse = await _unitOfWork.Warehouses.GetAsync(w => w.WarehouseID == warehouseId);
            int result = 0;
            if (warehouse != null)
            {
                _logger.LogWarning("Delete failed. Warehouse ID: {WarehouseId} not found.", warehouseId);
                return false;
            }
            if (warehouse.IsDeleted)
            {
                _logger.LogInformation("Warehouse ID: {WarehouseId} was already deleted. No changes made.", warehouseId);
                return false;
            }
            warehouse.IsDeleted = true;
            result = await _unitOfWork.SaveChangesAsync();
            if (result > 0)
            {
                _logger.LogInformation("Successfully soft-deleted Warehouse ID: {WarehouseId} in database.", warehouseId);
                await Task.WhenAll(
                    _cacheService.RemoveAsync(ALL_WAREHOUSES_KEY),
                    _cacheService.RemoveAsync($"{WAREHOUSE_KEY_PREFIX}{warehouseId}")
                );
                _logger.LogInformation("Cleared cache for Warehouse ID: {WarehouseId} and List.", warehouseId);
                return true;
            }
            else
            {
                _logger.LogError("Unexpected error: Changes were not saved for Warehouse ID: {WarehouseId}", warehouseId);
                return false;
            }
        }
        public async Task<ShowWareHouseResponseDTO?> SearchWarehouseAsync(int id)
        {
            // 1. kiểm tra cache trước tránh gọi db không cần thiết
            _logger.LogInformation("Searching for Warehouse with ID: {WarehouseID}", id);
            var cache = await _cacheService.GetAsync<ShowWareHouseResponseDTO>($"{WAREHOUSE_KEY_PREFIX}{id}");
            if (cache != null)
            {
                _logger.LogInformation("Found Warehouse in cache: {WarehouseID}", id);
                return cache;
            }
            _logger.LogInformation("Cache miss. Searching DB for Warehouse with ID: {WarehouseID}", id);
            // 2. nếu không có trong cache thì gọi db
            WarehouseEntity? warehouse = await _unitOfWork.Warehouses.GetAsync(
                w => w.WarehouseID == id && !w.IsDeleted,
                w => w.Manager!
            );

            ShowWareHouseResponseDTO? responseDTO = null;

            if (warehouse != null)
            {
                responseDTO = _mapper.Map<ShowWareHouseResponseDTO>(warehouse);
                // 3. Lưu vào Cache để lần sau không phải gọi DB nữa
                // và set thời gian hết hạn (expiry) tùy theo nghiệp vụ
                await _cacheService.SetAsync($"{WAREHOUSE_KEY_PREFIX}{id}", responseDTO, TimeSpan.FromMinutes(5));
                _logger.LogInformation("Added Warehouse to cache: {WarehouseID}", id);
            }
            return responseDTO;
        }

        public async Task<List<ShowWareHouseResponseDTO>> GetAllWarehousesAsync()
        {
            _logger.LogInformation("Getting all Warehouse");

            var cached = await _cacheService.GetAsync<List<ShowWareHouseResponseDTO>>(ALL_WAREHOUSES_KEY);

            if (cached != null)
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
            _logger.LogInformation("Request to update Warehouse ID: {WarehouseId}", id);
            WarehouseEntity? _warehouse = await _unitOfWork.Warehouses.GetAsync(w => w.WarehouseID == id);

            if (_warehouse == null)
            {
                _logger.LogWarning("Update failed. Warehouse ID: {WarehouseId} not found.", id);
                return null;
            }
            string oldName = _warehouse.Name;

            _mapper.Map(request, _warehouse);
            //WarehouseEntity newWarehouse = _mapper.Map<WarehouseEntity>(request);
            //_warehouse.Name = request.Name;
            //_warehouse.Address = request.Address;
            //_warehouse.LocationLong = request.LocationLong;
            //_warehouse.LocationLat = request.LocationLat;
            //_warehouse.IsDeleted = request.IsDeleted;

            int result = await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Successfully updated Warehouse ID: {Id} changed name from '{OldName}' to '{NewName}'",
            id, oldName, request.Name);

            if (result > 0)
            {
                _logger.LogInformation("Successfully updated Warehouse ID: {WarehouseId} in database.", id);
                await Task.WhenAll(
                        _cacheService.RemoveAsync(ALL_WAREHOUSES_KEY),
                        _cacheService.RemoveAsync($"{WAREHOUSE_KEY_PREFIX}{id}")
                );
                _logger.LogInformation("Cleared cache for Warehouse ID: {WarehouseId} and List.", id);
                return _mapper.Map<UpdateWarehouseResponseDTO>(_warehouse);
            }
            _logger.LogInformation("No changes detected for Warehouse ID: {WarehouseId}.", id);
            return null;

        }
    }
}
