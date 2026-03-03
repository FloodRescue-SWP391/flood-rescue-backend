using FloodRescue.Services.DTO.Request.IncidentReportRequest;
using FloodRescue.Services.DTO.Response.IncidentResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.Interface.IncidentReport
{
    public interface IIncidentReportService
    {
        /// <summary>
        /// Lấy danh sách sự cố đang chờ xử lý (Pending) - Cho Coordinator
        /// </summary>
        Task<List<PendingIncidentResponseDTO>> GetPendingIncidentsAsync();

        /// <summary>
        /// Lấy lịch sử sự cố đã xử lý (Resolved) - Cho Coordinator/Admin
        /// </summary>
        Task<List<IncidentHistoryResponseDTO>> GetIncidentHistoryAsync();

        /// <summary>
        /// Coordinator xử lý sự cố: đóng incident, hủy mission, giải phóng team, đưa request về Processing
        /// </summary>
        Task<(ResolvedIncidentResponseDTO? Data, string? ErrorMessage)> ResolveIncidentAsync(ResolvedIncidentRequestDTO request, Guid currentUserId);
    }
}
