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
        public async Task<ApiResponse<RegisterResponseDTO>> Register(RegisterRequestDTO request)
        {
            var result = await _registerService.RegisterAsync(request);
            if (!result.Success)
            {
                return ApiResponse<RegisterResponseDTO>.Fail(result.Message,400);
            }

            return ApiResponse<RegisterResponseDTO>.Ok(result.Data!,result.Message,200);
        }
    }
}
