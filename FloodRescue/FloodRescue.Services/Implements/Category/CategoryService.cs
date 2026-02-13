using AutoMapper;
using FloodRescue.Repositories.Entites;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.DTO.Request.Category;
using FloodRescue.Services.DTO.Response.Category;
using FloodRescue.Services.Implements.Warehouse;
using FloodRescue.Services.Interface.Cache;
using FloodRescue.Services.Interface.Category;
using Microsoft.Extensions.Logging;
using CategoryEntity = FloodRescue.Repositories.Entites.Category;

namespace FloodRescue.Services.Implements.Category
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CategoryService> _logger;
        private readonly ICacheService _cacheService;

        public CategoryService(IUnitOfWork unitOfWork, IMapper mapper, ICacheService cacheService, ILogger<CategoryService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _cacheService = cacheService;
        }

        private const string ALL_CATEGORIES_KEY = "category:all";
        private const string CATEGORY_KEY_PREFIX = "category:";

        public async Task<CategoryResponseDTO> CreateAsync(CreateCategoryRequestDTO request)
        {
            _logger.LogInformation("[CategoryService] Creating new Category. Name: {CategoryName}", request.CategoryName);
            var category = _mapper.Map<CategoryEntity>(request);
            await _unitOfWork.Categories.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("[CategoryService - Sql Server] Created Category with ID: {CategoryId}", category.CategoryID);
            CategoryResponseDTO responseDTO = _mapper.Map<CategoryResponseDTO>(category);
            await _cacheService.RemoveAsync(ALL_CATEGORIES_KEY);
            _logger.LogInformation("[CategoryService - Redis] Cleared cache for all Categories.");
            return responseDTO;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogInformation("[CategoryService] Deleting Category ID: {CategoryId}", id);
            var category = await _unitOfWork.Categories.GetAsync(c => c.CategoryID == id);
            if (category == null)
            {
                _logger.LogWarning("[CategoryService - Sql Server] Delete failed. Category ID: {CategoryId} not found.", id);
                return false;
            }
            if (!category.IsDeleted)
            {
                category.IsDeleted = true;
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("[CategoryService - Sql Server] Soft-deleted Category ID: {CategoryId}", id);

                await Task.WhenAll(
                            _cacheService.RemoveAsync(ALL_CATEGORIES_KEY),
                            _cacheService.RemoveAsync(CATEGORY_KEY_PREFIX + id)
                        );
                _logger.LogInformation("[CategoryService - Redis] Cleared cache for Category ID: {CategoryId} and list.", id);
                return true;
            }
            _logger.LogInformation("[CategoryService] Category ID: {CategoryId} already deleted. Skipped.", id);
            return false;
        }

        public async Task<List<CategoryResponseDTO>> GetAllAsync()
        {
            _logger.LogInformation("[CategoryService] Getting all categories.");
            var cache = await _cacheService.GetAsync<List<CategoryResponseDTO>>(ALL_CATEGORIES_KEY);
            if (cache != null)
            {
                _logger.LogInformation("[CategoryService - Redis] Cache hit for all categories.");
                return cache;
            }
            _logger.LogInformation("[CategoryService - Redis] Cache miss. Fetching from database.");
            var list = await _unitOfWork.Categories.GetAllAsync(c => !c.IsDeleted);
            List<CategoryResponseDTO> responseDTOs = _mapper.Map<List<CategoryResponseDTO>>(list);
            await _cacheService.SetAsync(ALL_CATEGORIES_KEY, responseDTOs, TimeSpan.FromMinutes(5));
            _logger.LogInformation("[CategoryService - Redis] Cached {Count} categories.", responseDTOs.Count);
            return responseDTOs;
        }

        public async Task<CategoryResponseDTO?> GetByIdAsync(int id)
        {
            _logger.LogInformation("[CategoryService] Searching Category ID: {CategoryId}", id);
            // 1. Kiểm tra Cache trước
            var cache = await _cacheService.GetAsync<CategoryResponseDTO>(CATEGORY_KEY_PREFIX + id);
            if (cache != null)
            {
                _logger.LogInformation("[CategoryService - Redis] Cache hit for Category ID: {CategoryId}", id);
                return cache;
            }
            _logger.LogInformation("[CategoryService - Redis] Cache miss for Category ID: {CategoryId}", id);
            // 2. Nếu Cache null -> Gọi DB
            var category = await _unitOfWork.Categories.GetAsync(c => c.CategoryID == id && !c.IsDeleted);
            if (category != null) 
            {
                var responseDTO = _mapper.Map<CategoryResponseDTO>(category);
                // 3. Lưu vào Cache
                await _cacheService.SetAsync(CATEGORY_KEY_PREFIX + id, responseDTO, TimeSpan.FromMinutes(5));
                _logger.LogInformation("[CategoryService - Redis] Cached Category ID: {CategoryId}", id);
                return responseDTO;
            };
            _logger.LogWarning("[CategoryService - Sql Server] Category ID: {CategoryId} not found.", id);
            return null;
        }

        public async Task<bool> UpdateAsync(int id, CreateCategoryRequestDTO request)
        {
            _logger.LogInformation("[CategoryService] Updating Category ID: {CategoryId}", id);
            var category = await _unitOfWork.Categories.GetAsync(c => c.CategoryID == id && !c.IsDeleted);
            if (category == null)
            {
                _logger.LogWarning("[CategoryService - Sql Server] Update failed. Category ID: {CategoryId} not found.", id);
                return false;
            }
            // Lưu tên cũ ra biến riêng (để dành ghi log)
            string oldName = category.CategoryName;

            category.CategoryName = request.CategoryName;
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("[CategoryService - Sql Server] Updated Category ID: {Id}. Name: '{OldName}' -> '{NewName}'",
         id, oldName, request.CategoryName);
            // Dùng Task.WhenAll để chạy song song 2 lệnh xóa cache cho nhanh
            await Task.WhenAll(
                    _cacheService.RemoveAsync(ALL_CATEGORIES_KEY),
                    _cacheService.RemoveAsync(CATEGORY_KEY_PREFIX + id)
            );
            _logger.LogInformation("[CategoryService - Redis] Cleared cache for Category ID: {CategoryId} and list.", id);
            return true;
        }
    }
}
