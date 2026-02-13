using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FloodRescue.Services.DTO.ReliefOrderRequest;
using FloodRescue.Services.DTO.Response.ReliefOrder;

namespace FloodRescue.Services.Interface.ReliefOrder
{
    public interface IReliefOrder
    {
        Task<ReliefOrderResponseDTO> CreateAsync(ReliefOrderRequestDTO request);
    }
}
