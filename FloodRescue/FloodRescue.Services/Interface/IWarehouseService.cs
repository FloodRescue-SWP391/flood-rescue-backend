using AutoMapper;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.DTO.Request.Warehouse;
using FloodRescue.Services.Implements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.Interface
{
    public interface IWarehouseService
    {
        public Task<bool> CreateWarehouseAsync(CreateWarehouseRequestDTO request);

    }
}
