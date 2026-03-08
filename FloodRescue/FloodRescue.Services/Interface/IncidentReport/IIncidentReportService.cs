using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.IncidentReportRequest;
using FloodRescue.Services.DTO.Request.RescueMissionRequest;
using FloodRescue.Services.DTO.Response.IncidentResponse;
using FloodRescue.Services.DTO.Response.RescueMissionResponse;
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
        /// Đội cứu hộ báo cáo sự cố trong khi đang thực hiện nhiệm vụ
        /// </summary>
        Task<(IncidentReportResponseDTO? Data, string? ErrorMessage)> ReportIncidentAsync(IncidentReportRequestDTO request, Guid currentUserId);

        /// <summary>
        /// Coordinator xử lý sự cố: đóng incident, hủy mission, giải phóng team, đưa request về Processing
        /// </summary>
        Task<(ResolvedIncidentResponseDTO? Data, string? ErrorMessage)> ResolveIncidentAsync(ResolvedIncidentRequestDTO request, Guid currentUserId);

        /// <summary>
        /// Lấy danh sách sự cố có lọc theo trạng thái, thời gian và phân trang
        /// </summary>
        Task<PagedResult<IncidentListResponseDTO>> GetFilteredIncidentsAsync(IncidentFilterDTO filter);

        /// <summary>
        /// Lấy chi tiết một sự cố theo ID - Cho Coordinator/Admin
        /// </summary>
        Task<IncidentDetailResponseDTO?> GetIncidentDetailByIdAsync(Guid incidentReportId);
    }
}
