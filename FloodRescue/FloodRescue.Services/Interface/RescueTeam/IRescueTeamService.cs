using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.RescueTeamRequest;
using FloodRescue.Services.DTO.Response.RescueTeamResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.Interface.RescueTeam
{
    public interface IRescueTeamService
    {
        Task<List<RescueTeamResponseDTO>> GetAllRescueTeamsAsync();
        Task<RescueTeamResponseDTO?> GetRescueTeamByIdAsync(Guid rescueTeamId);

        Task<RescueTeamResponseDTO> CreateRescueTeamAsync(RescueTeamRequestDTO rescueTeamDTO);

        Task<RescueTeamResponseDTO?> UpdateRescueTeamAsync(Guid rescueTeamId, RescueTeamRequestDTO rescueTeamDTO);
        Task<bool> DeleteRescueTeamAsync(Guid rescueTeamId);
        Task<PagedResult<RescueTeamResponseDTO>> GetFilteredRescueTeamsAsync(RescueTeamFilterDTO filter);
        Task<(RescueTeamByIdResponseDTO?, string? errorMessage)> GetTeamMembersByTeamID(Guid teamId);

    }
}
