using FloodRescue.Services.DTO.Request.InventoryRequest;
using FloodRescue.Services.DTO.Response.InventoryResponse;

namespace FloodRescue.Services.Interface.Inventory
{
    public interface IInventoryService
    {
        Task<(AdjustInventoryResponseDTO? Data, string? ErrorMessage)> AdjustInventoryAsync(AdjustInventoryRequestDTO request);
    }
}
