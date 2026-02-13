using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.Category;
using FloodRescue.Services.DTO.Response.Category;
using FloodRescue.Services.Interface.Category;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloodRescue.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ICategoryService categoryService, ILogger<CategoriesController> logger)
        {
            _categoryService = categoryService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<CategoryResponseDTO>>>> GetCategories()
        {
            _logger.LogInformation("[CategoriesController] GET all categories called.");
            try
            {
                var list = await _categoryService.GetAllAsync();
                _logger.LogInformation("[CategoriesController] Returned {Count} categories.", list.Count);
                return ApiResponse<List<CategoryResponseDTO>>.Ok(list, "Get categories successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CategoriesController - Error] GET all categories failed.");
                return StatusCode(500, ApiResponse<List<CategoryResponseDTO>>.Fail("Internal server error", 500));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<CategoryResponseDTO>>> GetCategory(int id)
        {
            _logger.LogInformation("[CategoriesController] GET category called. ID: {Id}", id);
            try
            {
                var category = await _categoryService.GetByIdAsync(id);
                if (category == null)
                {
                    _logger.LogWarning("[CategoriesController] Category ID: {Id} not found.", id);
                    return ApiResponse<CategoryResponseDTO>.Fail("Category not found", 404);
                }
                _logger.LogInformation("[CategoriesController] Category ID: {Id} returned.", id);
                return ApiResponse<CategoryResponseDTO>.Ok(category, "Get category successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CategoriesController - Error] GET category failed. ID: {Id}", id);
                return StatusCode(500, ApiResponse<CategoryResponseDTO>.Fail("Internal server error", 500));
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> PutCategory(int id, CreateCategoryRequestDTO request)
        {
            _logger.LogInformation("[CategoriesController] PUT category called. ID: {Id}", id);
            try
            {
                var result = await _categoryService.UpdateAsync(id, request);
                if (!result)
                {
                    _logger.LogWarning("[CategoriesController] Update category failed. ID: {Id}", id);
                    return ApiResponse<bool>.Fail("Update failed or category not found", 400);
                }
                _logger.LogInformation("[CategoriesController] Category ID: {Id} updated.", id);
                return ApiResponse<bool>.Ok(true, "Update category successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CategoriesController - Error] PUT category failed. ID: {Id}", id);
                return StatusCode(500, ApiResponse<bool>.Fail("Internal server error", 500));
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<CategoryResponseDTO>>> PostCategory(CreateCategoryRequestDTO request)
        {
            _logger.LogInformation("[CategoriesController] POST category called. Name: {Name}", request.CategoryName);
            try
            {
                var result = await _categoryService.CreateAsync(request);
                _logger.LogInformation("[CategoriesController] Category created. ID: {Id}", result.CategoryID);
                return ApiResponse<CategoryResponseDTO>.Ok(result, "Create category successfully", 201);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CategoriesController - Error] POST category failed. Name: {Name}", request.CategoryName);
                return StatusCode(500, ApiResponse<CategoryResponseDTO>.Fail("Internal server error", 500));
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteCategory(int id)
        {
            _logger.LogInformation("[CategoriesController] DELETE category called. ID: {Id}", id);
            try
            {
                var result = await _categoryService.DeleteAsync(id);
                if (!result)
                {
                    _logger.LogWarning("[CategoriesController] Delete category failed. ID: {Id}", id);
                    return ApiResponse<bool>.Fail("Delete failed or category not found", 400);
                }
                _logger.LogInformation("[CategoriesController] Category ID: {Id} deleted.", id);
                return ApiResponse<bool>.Ok(true, "Delete category successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CategoriesController - Error] DELETE category failed. ID: {Id}", id);
                return StatusCode(500, ApiResponse<bool>.Fail("Internal server error", 500));
            }
        }
    }
}
