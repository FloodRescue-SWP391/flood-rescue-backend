using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.Category;
using FloodRescue.Services.DTO.Response.Category;
using FloodRescue.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloodRescue.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // GET: api/Categories
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<CategoryResponseDTO>>>> GetCategories()
        {
            var list = await _categoryService.GetAllAsync();
            return ApiResponse<List<CategoryResponseDTO>>.Ok(list, "Get categories successfully", 200);
        }

        // GET: api/Categories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<CategoryResponseDTO>>> GetCategory(int id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            if (category == null)
            {
                return ApiResponse<CategoryResponseDTO>.Fail("Category not found", 404);
            }
            return ApiResponse<CategoryResponseDTO>.Ok(category, "Get category successfully", 200);
        }

        // PUT: api/Categories/5
        [HttpPut("{id}")]
        
        public async Task<ActionResult<ApiResponse<bool>>> PutCategory(int id, CreateCategoryRequestDTO request)
        {
            var result = await _categoryService.UpdateAsync(id, request);
            if (!result)
            {
                return ApiResponse<bool>.Fail("Update failed or category not found", 400);
            }
            return ApiResponse<bool>.Ok(true, "Update category successfully", 200);
        }

        // POST: api/Categories
        [HttpPost]
        
        public async Task<ActionResult<ApiResponse<CategoryResponseDTO>>> PostCategory(CreateCategoryRequestDTO request)
        {
            var result = await _categoryService.CreateAsync(request);
            return ApiResponse<CategoryResponseDTO>.Ok(result, "Create category successfully", 201);
        }

        // DELETE: api/Categories/5
        [HttpDelete("{id}")]
       
        public async Task<ActionResult<ApiResponse<bool>>> DeleteCategory(int id)
        {
            var result = await _categoryService.DeleteAsync(id);
            if (!result)
            {
                return ApiResponse<bool>.Fail("Delete failed or category not found", 400);
            }
            return ApiResponse<bool>.Ok(true, "Delete category successfully", 200);
        }
    }
}
