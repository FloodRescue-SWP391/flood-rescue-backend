using AutoMapper;
using FloodRescue.Repositories.Entites;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.DTO.Request.Category;
using FloodRescue.Services.DTO.Response.Category;
using FloodRescue.Services.Interface;

namespace FloodRescue.Services.Implements
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CategoryService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<CategoryResponseDTO> CreateAsync(CreateCategoryRequestDTO request)
        {
            var category = _mapper.Map<Category>(request);
            await _unitOfWork.Categories.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<CategoryResponseDTO>(category);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _unitOfWork.Categories.GetAsync(c => c.CategoryID == id);
            if (category == null) return false;
            if (!category.IsDeleted)
            {
                category.IsDeleted = true;
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<List<CategoryResponseDTO>> GetAllAsync()
        {
            var list = await _unitOfWork.Categories.GetAllAsync(c => !c.IsDeleted);
            return _mapper.Map<List<CategoryResponseDTO>>(list);
        }

        public async Task<CategoryResponseDTO?> GetByIdAsync(int id)
        {
            var category = await _unitOfWork.Categories.GetAsync(c => c.CategoryID == id && !c.IsDeleted);
            return category == null ? null : _mapper.Map<CategoryResponseDTO>(category);
        }

        public async Task<bool> UpdateAsync(int id, CreateCategoryRequestDTO request)
        {
            var category = await _unitOfWork.Categories.GetAsync(c => c.CategoryID == id && !c.IsDeleted);
            if (category == null) return false;
            category.CategoryName = request.CategoryName;
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
