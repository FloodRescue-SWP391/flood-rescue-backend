using FloodRescue.Services.DTO.Request.InventoryRequest;
using FloodRescue.Services.DTO.Response.InventoryResponse;
﻿using FloodRescue.Services.DTO.Request.InventoryRequest;
using FloodRescue.Services.DTO.Response.InventoryResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.Interface.Inventory
{
    public interface IInventoryService
    {
        Task<(AdjustInventoryResponseDTO? Data, string? ErrorMessage)> AdjustInventoryAsync(AdjustInventoryRequestDTO request);
        /// <summary>
        /// Nhập hàng vào kho - cộng dồn hoặc tạo mới inventory
        /// </summary>
        Task<(ReceiveInventoryResponseDTO? Data, string? ErrorMessage)> ReceiveInventoryAsync(ReceiveInventoryRequestDTO request);
        /// <summary>
        /// Lấy danh sách tồn kho của một kho cụ thể
        /// </summary>
        Task<(List<InventoryItemResponseDTO>? Data, string? ErrorMessage)> GetInventoryByWarehouseAsync(int warehouseId);
    }
}
