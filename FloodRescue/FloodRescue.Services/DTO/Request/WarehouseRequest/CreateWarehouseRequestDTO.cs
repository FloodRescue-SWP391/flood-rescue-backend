using FloodRescue.Repositories.Entites;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Request.Warehouse
{
    public class CreateWarehouseRequestDTO
    {
        public Guid ManagerID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public double LocationLong { get; set; }
        public double LocationLat { get; set; }
    }
}
