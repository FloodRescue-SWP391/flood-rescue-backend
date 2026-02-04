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

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<RegisterResponseDTO>>> Register([FromBody] RegisterRequestDTO request)
        {
            //  Destructure tuple
            var (data, errorMessage) = await _authService.RegisterAsync(request);
            if (data == null)
            {
                return ApiResponse<RegisterResponseDTO>.Fail(errorMessage!, 400);
            }
            // Message: Register successfully được tạo ở đây vì bên RegisterService chỉ trả về data và errorMessage nếu mà để message thành công mà vô biến errorMessage thì không hợp lý
            return ApiResponse<RegisterResponseDTO>.Ok(data!, "Register successfully", 201);
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<AuthResponseDTO>>> Login([FromBody] LoginRequestDTO request)
        {
            var (data, errorMessage) = await _authService.LoginAsync(request);

            if (data == null)
            {
                return ApiResponse<AuthResponseDTO>.Fail(errorMessage!, 401);
            }

            return ApiResponse<AuthResponseDTO>.Ok(data: data!, message: "Login successfully", 200);
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<ApiResponse<AuthResponseDTO>>> RefreshToken(RefreshTokenRequest request)
        {
            var (data, errorMessage) = await _authService.RefeshTokenAsync(request);

            if (data == null)
            {
                return ApiResponse<AuthResponseDTO>.Fail(errorMessage!, 401);
            }

            return ApiResponse<AuthResponseDTO>.Ok(data: data!, message: "Token refreshed successfully", 200);
        }
    }
}
