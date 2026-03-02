using FloodRescue.Services.DTO.Request.RescueMissionRequest;
using FloodRescue.Services.DTO.Response.RescueMissionResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.Interface.RescueMission
{
    public interface IRescueMissionService
    {
        Task<DispatchMissionResponseDTO?> DispatchMissionAsync(DispatchMissionRequestDTO request);
        Task<RespondMissionResponseDTO?> RespondMissionAsync(RespondMessageRequestDTO request);
        Task<CompleteMissionResponseDTO?> CompleteMissionAsync(CompleteMissionRequestDTO request);

        /// <summary>
        /// Lấy danh sách các nhiệm vụ đang chờ (Assigned) cho đội cứu hộ dựa vào CurrentUserID
        /// </summary>
        Task<(List<PendingMissionResponseDTO>? Data, string? ErrorMessage)> GetPendingMissionsAsync(Guid currentUserId);
        Task<ConfirmPickupResponseDTO?> ConfirmPickupAsync(ConfirmPickUpRequestDTO request);
    }
}
