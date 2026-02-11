using AutoMapper;
using FloodRescue.Services.DTO.Request.RescueRequest;
using FloodRescue.Services.Mapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RescueService.Tests.Helpers
{
    public static class MapperFactory
    {
        public static IMapper CreateMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();

                // IMPORTANT: Service map RescueRequest(entity) -> RescueRequestKafkaMessage
                // MappingProfile hiện thiếu map này => add tại đây để test chạy ổn.
                cfg.CreateMap<FloodRescue.Repositories.Entites.RescueRequest, RescueRequestKafkaMessage>();
            });

            return config.CreateMapper();
        }
    }
}
