using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.UserRequest;
using FloodRescue.Services.DTO.Response.UserResponse;
using FloodRescue.Services.Interface.UserManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

        /// <summary>
        /// Admin khóa tài khoản nhân sự (Soft Delete)
        /// PATCH /api/users/{userId}/deactivate
        /// </summary>
        [HttpPatch("{userId}/deactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<string>>> DeactivateUser(Guid userId)
        {
            _logger.LogInformation("[UsersController] PATCH deactivate user called. UserID: {UserID}", userId);

            try
            {
                // Lấy CurrentUserID từ JWT Token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");

                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid currentUserId))
                {
                    _logger.LogWarning("[UsersController] Unable to extract UserID from JWT token.");
                    return Unauthorized(ApiResponse<string>.Fail("Invalid token. Please login again.", 401));
                }

                _logger.LogInformation("[UsersController] DeactivateUser by AdminID: {AdminID} for UserID: {UserID}", currentUserId, userId);

                var (success, errorMessage) = await _userService.DeactivateUserAsync(userId, currentUserId);

                if (!success)
                {
                    _logger.LogWarning("[UsersController] DeactivateUser failed. UserID: {UserID}. Error: {Error}", userId, errorMessage);
                    return BadRequest(ApiResponse<string>.Fail(errorMessage ?? "Failed to deactivate user.", 400));
                }

                _logger.LogInformation("[UsersController] DeactivateUser success. UserID: {UserID}", userId);
                return Ok(ApiResponse<string>.Ok("Đã khóa tài khoản nhân sự thành công.", "Đã khóa tài khoản nhân sự thành công.", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UsersController - Error] DeactivateUser failed. UserID: {UserID}", userId);
                return StatusCode(500, ApiResponse<string>.Fail("Internal server error", 500));
            }
        }
    }
}
