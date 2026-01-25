using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.RegisterRequest;
using FloodRescue.Services.DTO.Response.RegisterResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.Interface
{
    public interface IRegisterService
    {
        Task<(RegisterResponseDTO? Data, string? ErrorMessage)> RegisterAsync(RegisterRequestDTO request);
    }
}
