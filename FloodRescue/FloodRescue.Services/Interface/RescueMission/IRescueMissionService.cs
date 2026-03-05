using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.RescueMissionRequest;
using FloodRescue.Services.DTO.Response.RescueMissionResponse;
using FloodRescue.Services.DTO.Response.RescueTeamResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.Interface.RescueMission
{
    public interface IRescueMissionService
    {
        Task<DispatchMissionResponseDTO?> DispatchMissionAsync(DispatchMissionRequestDTO request, Guid coordinatorID);
        Task<RespondMissionResponseDTO?> RespondMissionAsync(RespondMessageRequestDTO request);
        Task<CompleteMissionResponseDTO?> CompleteMissionAsync(CompleteMissionRequestDTO request);

        /// <summary>
        /// Lấy danh sách các nhiệm vụ đang chờ (Assigned) cho đội cứu hộ dựa vào CurrentUserID
        /// </summary>
        Task<(List<PendingMissionResponseDTO>? Data, string? ErrorMessage)> GetPendingMissionsAsync(Guid currentUserId);
        Task<ConfirmPickupResponseDTO?> ConfirmPickupAsync(ConfirmPickUpRequestDTO request);
        Task<PagedResult<RescueMissionListResponseDTO>> GetFilteredMissionAsync(RescueMissionFilterDTO filter);

        /// <summary>
        /// Lấy chi tiết một nhiệm vụ theo ID - Cho Coordinator/Admin/RescueTeam (Team chỉ xem của mình)
        /// </summary>
        Task<(RescueMissionDetailResponseDTO? Data, string? ErrorMessage)> GetMissionDetailByIdAsync(Guid missionId, Guid currentUserId, string userRole);

        /// <summary>
        /// Lấy danh sách thành viên của một đội cứu hộ (Đội trưởng luôn ở đầu)
        /// </summary>
        Task<(List<RescueTeamMemberResponseDTO>? Data, string? ErrorMessage)> GetTeamMembersAsync(Guid teamId, Guid currentUserId, string userRole);
    }
}
