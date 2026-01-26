using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.Auth;
using FloodRescue.Services.DTO.Request.AuthRequest;
using FloodRescue.Services.DTO.Response.AuthResponse;
using FloodRescue.Services.DTO.Response.RegisterResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.Interface
{
    public interface IAuthService
    {
        /// <summary>
        /// Đăng kí người dùng mới
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<(RegisterResponseDTO? Data, string? ErrorMessage)> RegisterAsync(RegisterRequestDTO request);

        /// <summary>
        /// Lấy access token mới
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<(AuthResponseDTO? Data, string? ErrorMessage)> RefeshTokenAsync(RefreshTokenRequest request);    

        /// <summary>
        /// Login bằng username/password
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<(AuthResponseDTO? Data, string? ErrorMessage)> LoginAsync(LoginRequestDTO request);
    }
}
