using FloodRescue.Services.DTO.Request.RescueRequest;
using FloodRescue.Services.DTO.Response.RescueRequestResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.Interface.RescueRequest
{
    public interface IRescueRequestService
    {
        /// <summary>
        /// Tạo rescue request mới với status = Pending
        /// Lưu request + images vào DB trong 1 transaction
        /// </summary>
        Task<(CreateRescueRequestResponseDTO? Data, string? ErrorMessage)> CreateRescueRequestAsync(CreateRescueRequestDTO request);


        /// <summary>
        /// Citizen tra cứu trạng thái request bằng ShortCode (có cache)
        /// </summary>
        Task<CreateRescueRequestResponseDTO?> GetByShortCodeAsync(string shortCode);

        /// <summary>
        /// Coordinator xem danh sách tất cả rescue requests (có cache)
        /// </summary>
        Task<List<CreateRescueRequestResponseDTO>> GetAllRescueRequestsAsync();
    }
}
