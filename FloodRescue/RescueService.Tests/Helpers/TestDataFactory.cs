using FloodRescue.Services.DTO.Request.RescueRequest;
using FloodRescue.Services.SharedSetting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RescueService.Tests.Helpers
{
    public static class TestDataFactory
    {
        public static CreateRescueRequestDTO ValidCreateRescueRequestDto(List<string>? urls = null)
        {
            return new CreateRescueRequestDTO
            {
                RequestType = RescueRequestType.RESCUE_TYPE,
                Description = "Help",
                LocationLatitude = 10.77,
                LocationLongitude = 106.69,
                PhoneNumber = "0909123456",
                ImageUrls = urls ?? new List<string> { "https://img/1.png", "https://img/2.png" }
            };
        }
    }
}
