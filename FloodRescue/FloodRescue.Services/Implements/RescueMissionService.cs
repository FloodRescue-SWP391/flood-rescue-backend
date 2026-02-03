using AutoMapper;
using FloodRescue.Repositories.Entites;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.DTO.Request.RescueMission;
using FloodRescue.Services.DTO.Response.RescueMission;
using FloodRescue.Services.Interface;

namespace FloodRescue.Services.Implements
{
    public class RescueMissionService : IRescueMissionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public RescueMissionService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<List<RescueMissionResponseDTO>> GetAllAsync()
        {
            var list = await _unitOfWork.RescueMissions.GetAllAsync(m => !m.IsDeleted);
            return _mapper.Map<List<RescueMissionResponseDTO>>(list);
        }

        public async Task<RescueMissionResponseDTO?> GetByIdAsync(Guid id)
        {
            var mission = await _unitOfWork.RescueMissions.GetAsync(m => m.RescueMissionID == id && !m.IsDeleted);
            return mission == null ? null : _mapper.Map<RescueMissionResponseDTO>(mission);
        }

        public async Task<RescueMissionResponseDTO> CreateAsync(CreateRescueMissionRequestDTO request)
        {
            var mission = _mapper.Map<RescueMission>(request);

            // Entity default AssignedAt = UtcNow, nh?ng n?u client có truy?n thì ?u tiên d? li?u client
            if (request.AssignedAt.HasValue)
            {
                mission.AssignedAt = request.AssignedAt.Value;
            }

            await _unitOfWork.RescueMissions.AddAsync(mission);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<RescueMissionResponseDTO>(mission);
        }

        public async Task<bool> UpdateAsync(Guid id, CreateRescueMissionRequestDTO request)
        {
            var mission = await _unitOfWork.RescueMissions.GetAsync(m => m.RescueMissionID == id && !m.IsDeleted);
            if (mission == null) return false;

            mission.RescueTeamID = request.RescueTeamID;
            mission.RescueRequestID = request.RescueRequestID;
            mission.Status = request.Status;

            if (request.AssignedAt.HasValue) mission.AssignedAt = request.AssignedAt.Value;
            mission.StartTime = request.StartTime;
            mission.EndTime = request.EndTime;

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var mission = await _unitOfWork.RescueMissions.GetAsync(m => m.RescueMissionID == id);
            if (mission == null) return false;

            if (!mission.IsDeleted)
            {
                mission.IsDeleted = true;
                await _unitOfWork.SaveChangesAsync();
                return true;
            }

            return false;
        }
    }
}