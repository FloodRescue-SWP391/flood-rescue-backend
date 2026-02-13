using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.Auth;
using FloodRescue.Services.DTO.Request.AuthRequest;
using FloodRescue.Services.DTO.Response.AuthResponse;
using FloodRescue.Services.DTO.Response.RegisterResponse;
using FloodRescue.Services.Interface.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FloodRescue.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<RegisterResponseDTO>>> Register([FromBody] RegisterRequestDTO request)
        {
            _logger.LogInformation("[AuthController] POST register called. Username: {Username}", request.Username);
            try
            {
                var (data, errorMessage) = await _authService.RegisterAsync(request);
                if (data == null)
                {
                    _logger.LogWarning("[AuthController] Register failed. Username: {Username}, Error: {Error}", request.Username, errorMessage);
                    return ApiResponse<RegisterResponseDTO>.Fail(errorMessage!, 400);
                }
                _logger.LogInformation("[AuthController] Register success. UserID: {UserID}, Username: {Username}", data.UserID, data.Username);
                return ApiResponse<RegisterResponseDTO>.Ok(data!, "Register successfully", 201);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AuthController - Error] Register failed. Username: {Username}", request.Username);
                return StatusCode(500, ApiResponse<RegisterResponseDTO>.Fail("Internal server error", 500));
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<AuthResponseDTO>>> Login([FromBody] LoginRequestDTO request)
        {
            _logger.LogInformation("[AuthController] POST login called. Username: {Username}", request.Username);
            try
            {
                var (data, errorMessage) = await _authService.LoginAsync(request);

                if (data == null)
                {
                    _logger.LogWarning("[AuthController] Login failed. Username: {Username}, Error: {Error}", request.Username, errorMessage);
                    return ApiResponse<AuthResponseDTO>.Fail(errorMessage!, 401);
                }

                _logger.LogInformation("[AuthController] Login success. UserID: {UserID}, Username: {Username}", data.UserID, data.Username);
                return ApiResponse<AuthResponseDTO>.Ok(data: data!, message: "Login successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AuthController - Error] Login failed. Username: {Username}", request.Username);
                return StatusCode(500, ApiResponse<AuthResponseDTO>.Fail("Internal server error", 500));
            }
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<ApiResponse<AuthResponseDTO>>> RefreshToken(RefreshTokenRequest request)
        {
            _logger.LogInformation("[AuthController] POST refresh-token called.");
            try
            {
                var (data, errorMessage) = await _authService.RefeshTokenAsync(request);

                if (data == null)
                {
                    _logger.LogWarning("[AuthController] Refresh token failed. Error: {Error}", errorMessage);
                    return ApiResponse<AuthResponseDTO>.Fail(errorMessage!, 401);
                }

                _logger.LogInformation("[AuthController] Refresh token success. UserID: {UserID}", data.UserID);
                return ApiResponse<AuthResponseDTO>.Ok(data: data!, message: "Token refreshed successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AuthController - Error] Refresh token failed.");
                return StatusCode(500, ApiResponse<AuthResponseDTO>.Fail("Internal server error", 500));
            }
        }
    }
}
