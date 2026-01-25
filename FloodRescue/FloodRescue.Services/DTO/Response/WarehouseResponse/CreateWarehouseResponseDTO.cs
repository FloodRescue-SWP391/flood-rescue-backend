using FloodRescue.Repositories.Entites;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Response.Warehouse
{
    public class CreateWarehouseResponseDTO
    {
     
        public string Name { get; set; } = string.Empty;

        public string? Address { get; set; }

        public double LocationLong { get; set; }

        public double LocationLat { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
    }
}
