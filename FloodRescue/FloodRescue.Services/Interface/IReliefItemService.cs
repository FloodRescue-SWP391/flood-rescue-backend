using FloodRescue.Services.DTO.Request.ReliefItem;
using FloodRescue.Services.DTO.Response.ReliefItem;

namespace FloodRescue.Services.Interface
{
    public interface IReliefItemService
    {
        Task<List<ReliefItemResponseDTO>> GetAllAsync();
        Task<ReliefItemResponseDTO?> GetByIdAsync(int id);
        Task<ReliefItemResponseDTO> CreateAsync(CreateReliefItemRequestDTO request);
        Task<bool> UpdateAsync(int id, CreateReliefItemRequestDTO request);
        Task<bool> DeleteAsync(int id);
    }
}
