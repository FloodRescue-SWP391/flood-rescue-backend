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
            Warehouse? warehouse = await _unitOfWork.Warehouses.GetAsync(w => w.WarehouseID == warehouseId);
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
            Warehouse? warehouse = await _unitOfWork.Warehouses.GetAsync(
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
            List<Warehouse> warehouse = await _unitOfWork.Warehouses.GetAllAsync(
                w => !w.IsDeleted,
                w => w.Manager!
            );
            return _mapper.Map<List<ShowWareHouseResponseDTO>>(warehouse);
        }
        public async Task<UpdateWarehouseResponseDTO> UpdateWarehouseAsync(int id, UpdateWarehouseRequestDTO request)
        {
            Warehouse? _warehouse = await _unitOfWork.Warehouses.GetAsync(w => w.WarehouseID == id);

            if (_warehouse == null)
            {
                return null;
            }

            Warehouse newWarehouse = _mapper.Map<Warehouse>(request);
            //_warehouse.Name = request.Name;
            //_warehouse.Address = request.Address;
            //_warehouse.LocationLong = request.LocationLong;
            //_warehouse.LocationLat = request.LocationLat;
            //_warehouse.IsDeleted = request.IsDeleted;

            int result = await _unitOfWork.SaveChangesAsync();

            if (result > 0)
            {
                return _mapper.Map<UpdateWarehouseResponseDTO>(newWarehouse);
            }
            return null;
        }
    }
}
