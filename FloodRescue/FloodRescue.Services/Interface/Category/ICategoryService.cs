using FloodRescue.Services.DTO.Request.Category;
using FloodRescue.Services.DTO.Response.Category;

namespace FloodRescue.Services.Interface.Category
{
    public interface ICategoryService
    {
        Task<List<CategoryResponseDTO>> GetAllAsync();
        Task<CategoryResponseDTO?> GetByIdAsync(int id);
        Task<CategoryResponseDTO> CreateAsync(CreateCategoryRequestDTO request);
        Task<bool> UpdateAsync(int id, CreateCategoryRequestDTO request);
        Task<bool> DeleteAsync(int id);
    }
}
