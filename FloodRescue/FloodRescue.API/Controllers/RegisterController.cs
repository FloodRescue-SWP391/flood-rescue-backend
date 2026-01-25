using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.RegisterRequest;
using FloodRescue.Services.DTO.Response.RegisterResponse;
using FloodRescue.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FloodRescue.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegisterController : ControllerBase
    {
        private readonly IRegisterService _registerService;

        public RegisterController(IRegisterService registerService)
        {
            _registerService = registerService;
        }

        [HttpPost("register")]
        public async Task<ApiResponse<RegisterResponseDTO>> Register([FromBody]RegisterRequestDTO request)
        {
            //  Destructure tuple
            var (data, errorMessage) = await _registerService.RegisterAsync(request);
            if (data == null)
            {
                return ApiResponse<RegisterResponseDTO>.Fail(errorMessage!, 400);
            }
            // Message: Register successfully được tạo ở đây vì bên RegisterService chỉ trả về data và errorMessage nếu mà để message thành công mà vô biến errorMessage thì không hợp lý
            return ApiResponse<RegisterResponseDTO>.Ok(data!, "Register successfully", 201);
        }
    }
}
