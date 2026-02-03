using FloodRescue.Services.DTO.Request.RescueRequestRequest;
using FloodRescue.Services.DTO.Response.RescueRequestResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.Interface
{
    public interface IRescueRequestService
    {
        Task<(RescueRequestResponseDTO? data, string? errorMessage)> CreateRescueRequestAsync(RescueRequestRequestDTO request);

        Task<bool> DeleteRescueRequestAsync(Guid id);

        Task<RescueRequestResponseDTO?> GetRescueRequestByIdAsync(Guid id);

        Task<List<RescueRequestResponseDTO>> GetAllRescueRequestsAsync();

        Task<(RescueRequestResponseDTO? data, string? errorMessage)> UpdateRescueRequestAsync(Guid id, RescueRequestRequestDTO request);

    }
}
