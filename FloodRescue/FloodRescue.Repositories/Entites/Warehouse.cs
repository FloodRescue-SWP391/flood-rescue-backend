using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FloodRescue.Repositories.Entites
{
    [Table("Warehouses")]
    public class Warehouse
    {
        [Column("WarehouseID", TypeName = "int")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int WarehouseID { get; set; }
        [Column("ManagerID", TypeName = "uniqueidentifier")]
        public Guid ManagerID { get; set; }
        [ForeignKey(nameof(ManagerID))]
        [JsonIgnore]
        public User? Manager { get; set; }
        [Column("Name", TypeName = "nvarchar(200)'")]
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        [Column("Address", TypeName = "nvarchar(255)")]
        [MaxLength(255)]
        public string? Address { get; set; }
        [Column("LocationLong", TypeName = "float")]
        public double LocationLong { get; set; }
        [Column("LocationLat", TypeName = "float")]
        public double LocationLat { get; set; }
        [Column("IsDeleted", TypeName = "BIT")]
        public bool IsDeleted { get; set; }


    }
}
