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
    }
}
