using AutoMapper;
using FloodRescue.Repositories.Entites;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.DTO.Request.Warehouse;
using FloodRescue.Services.DTO.Request.WarehouseRequest;
using FloodRescue.Services.DTO.Response.Warehouse;
using FloodRescue.Services.Implements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.Interface.Warehouse
{
    public interface IWarehouseService
    {
        public Task<CreateWarehouseResponseDTO> CreateWarehouseAsync(CreateWarehouseRequestDTO request);

        //public Task<bool> UpdateWarehouseAsync(UpdateWarehouseRequestDTO request);

        public Task<bool> DeleteWarehouseAsync(int warehouseId);

        public Task<ShowWareHouseResponseDTO> SearchWarehouseAsync(int id);

        public Task<List<ShowWareHouseResponseDTO>> GetAllWarehousesAsync();

        public Task<UpdateWarehouseResponseDTO?> UpdateWarehouseAsync(int id, UpdateWarehouseRequestDTO warehouse);
    }
}
