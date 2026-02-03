using FloodRescue.Services.DTO.Request.RescueMission;
using FloodRescue.Services.DTO.Response.RescueMission;

namespace FloodRescue.Services.Interface
{
    public interface IRescueMissionService
    {
        Task<List<RescueMissionResponseDTO>> GetAllAsync();
        Task<RescueMissionResponseDTO?> GetByIdAsync(Guid id);
        Task<RescueMissionResponseDTO> CreateAsync(CreateRescueMissionRequestDTO request);
        Task<bool> UpdateAsync(Guid id, CreateRescueMissionRequestDTO request);
        Task<bool> DeleteAsync(Guid id);
    }
}