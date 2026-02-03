using FloodRescue.Services.DTO.Request.IncidentReport;
using FloodRescue.Services.DTO.Response.IncidentReport;

namespace FloodRescue.Services.Interface
{
    public interface IIncidentReportService
    {
        Task<List<IncidentReportResponseDTO>> GetAllAsync();
        Task<IncidentReportResponseDTO?> GetByIdAsync(Guid id);
        Task<IncidentReportResponseDTO> CreateAsync(CreateIncidentReportRequestDTO request);
        Task<bool> UpdateAsync(Guid id, CreateIncidentReportRequestDTO request);
        Task<bool> DeleteAsync(Guid id);
    }
}