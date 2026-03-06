using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.UserRequest;
using FloodRescue.Services.DTO.Response.UserResponse;
using FloodRescue.Services.Interface.UserManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloodRescue.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Admin cập nhật thông tin nhân sự hệ thống (FullName, Phone, RoleID)
        /// PUT /api/users/{userId}
        /// </summary>
        [HttpPut("{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<UpdateUserResponseDTO>>> UpdateSystemUser(Guid userId, [FromBody] UpdateUserRequestDTO request)
        {
            _logger.LogInformation("[UsersController] PUT update user called. UserID: {UserID}", userId);

            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("[UsersController] UpdateSystemUser validation failed. ModelState invalid.");
                    return BadRequest(ApiResponse<UpdateUserResponseDTO>.Fail("Data is not valid, please check again.", 400));
                }

                var (data, errorMessage) = await _userService.UpdateSystemUserAsync(userId, request);

                if (data == null)
                {
                    _logger.LogWarning("[UsersController] UpdateSystemUser returned null. UserID: {UserID}. Error: {Error}", userId, errorMessage);
                    return BadRequest(ApiResponse<UpdateUserResponseDTO>.Fail(errorMessage ?? "Failed to update user.", 400));
                }

                _logger.LogInformation("[UsersController] UpdateSystemUser success. UserID: {UserID}", userId);
                return Ok(ApiResponse<UpdateUserResponseDTO>.Ok(data, "Cập nhật thông tin nhân sự thành công.", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UsersController - Error] UpdateSystemUser failed. UserID: {UserID}", userId);
                return StatusCode(500, ApiResponse<UpdateUserResponseDTO>.Fail("Internal server error", 500));
            }
        }
    }
}
