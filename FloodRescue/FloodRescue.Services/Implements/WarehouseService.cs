using AutoMapper;
using FloodRescue.Repositories.Entites;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.DTO.Request.Warehouse;
using FloodRescue.Services.DTO.Request.WarehouseRequest;
using FloodRescue.Services.DTO.Response.Warehouse;
using FloodRescue.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.Implements
{
    public class WarehouseService : IWarehouseService
    {
        IUnitOfWork _unitOfWork;
        IMapper _mapper;

        public WarehouseService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<CreateWarehouseResponseDTO> CreateWarehouseAsync(CreateWarehouseRequestDTO request)
        {
            Warehouse warehouse = _mapper.Map<Warehouse>(request);
            await _unitOfWork.Warehouses.AddAsync(warehouse);
            await _unitOfWork.SaveChangesAsync();

            CreateWarehouseResponseDTO responseDTO = _mapper.Map<CreateWarehouseResponseDTO>(warehouse);
            return responseDTO;
        }
        public async Task<bool> DeleteWarehouseAsync(int warehouseId)
        {
            Warehouse? warehouse = await _unitOfWork.Warehouses.GetByIdAsync(warehouseId);
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
            Warehouse? warehouse = await _unitOfWork.Warehouses.GetByIdAsync(id);
            if (warehouse != null)
            {
                responseDTO = _mapper.Map<ShowWareHouseResponseDTO>(warehouse);
            }
            return responseDTO;
        }
        public async Task<List<ShowWareHouseResponseDTO>> GetAllWarehousesAsync()
        {
            List<Warehouse> warehouse = await _unitOfWork.Warehouses.GetAllAsync();
            return _mapper.Map<List<ShowWareHouseResponseDTO>>(warehouse);
        }
        public async Task<UpdateWarehouseResponseDTO> UpdateWarehouseAsync(int id, UpdateWarehouseRequestDTO warehouse)
        {
            Warehouse? _warehouse = await _unitOfWork.Warehouses.GetByIdAsync(id);

            if (_warehouse == null)
            {
                return null;
            }

            _warehouse.Name = warehouse.Name;
            _warehouse.Address = warehouse.Address;
            _warehouse.LocationLong = warehouse.LocationLong;
            _warehouse.LocationLat = warehouse.LocationLat;
            _warehouse.IsDeleted = warehouse.IsDeleted;

            int result = await _unitOfWork.SaveChangesAsync();

            if (result > 0)
            {
                return _mapper.Map<UpdateWarehouseResponseDTO>(_warehouse);
            }
            return null;
        }
    }
}
