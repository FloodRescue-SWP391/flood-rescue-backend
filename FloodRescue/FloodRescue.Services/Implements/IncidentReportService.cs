using AutoMapper;
using FloodRescue.Repositories.Entites;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.DTO.Request.IncidentReport;
using FloodRescue.Services.DTO.Response.IncidentReport;
using FloodRescue.Services.Interface;

namespace FloodRescue.Services.Implements
{
    public class IncidentReportService : IIncidentReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public IncidentReportService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<List<IncidentReportResponseDTO>> GetAllAsync()
        {
            var list = await _unitOfWork.IncidentReports.GetAllAsync();
            return _mapper.Map<List<IncidentReportResponseDTO>>(list);
        }

        public async Task<IncidentReportResponseDTO?> GetByIdAsync(Guid id)
        {
            var report = await _unitOfWork.IncidentReports.GetAsync(r => r.IncidentReportID == id);
            return report == null ? null : _mapper.Map<IncidentReportResponseDTO>(report);
        }

        public async Task<IncidentReportResponseDTO> CreateAsync(CreateIncidentReportRequestDTO request)
        {
            var report = _mapper.Map<IncidentReport>(request);

            // entity default CreatedTime = UtcNow, gi? nguyên
            await _unitOfWork.IncidentReports.AddAsync(report);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<IncidentReportResponseDTO>(report);
        }

        public async Task<bool> UpdateAsync(Guid id, CreateIncidentReportRequestDTO request)
        {
            var report = await _unitOfWork.IncidentReports.GetAsync(r => r.IncidentReportID == id);
            if (report == null) return false;

            report.RescueMissionID = request.RescueMissionID;
            report.ReportedID = request.ReportedID;
            report.ResolvedBy = request.ResolvedBy;

            report.ResolvedTime = request.ResolvedTime;
            report.Title = request.Title;
            report.Latitiude = request.Latitiude;
            report.Longitude = request.Longitude;
            report.Status = request.Status;
            report.Description = request.Description;
            report.CoordinatorNote = request.CoordinatorNote;

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var report = await _unitOfWork.IncidentReports.GetAsync(r => r.IncidentReportID == id);
            if (report == null) return false;

            _unitOfWork.IncidentReports.Delete(report);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}