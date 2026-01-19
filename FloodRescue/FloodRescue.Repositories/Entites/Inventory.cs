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
    [Table("Inventories")]
    public class Inventory
    {
        [Column("InventoryID", TypeName = "uniqueidentifier")]
        [Key]
        public Guid InventoryID { get; set; } = Guid.NewGuid();
        [Column("ReliefItemID", TypeName = "int")]
        [Required]
        public int ReliefItemID { get; set; }
        [ForeignKey(nameof(ReliefItemID))]
        [JsonIgnore]
        public ReliefItem? ReliefItem { get; set; }
        [Column("WarehouseID", TypeName = "int")]
        [Required]
        public int WarehouseID { get; set; }    
        [ForeignKey(nameof(WarehouseID))]
        [JsonIgnore]
        public Warehouse? Warehouse { get; set; }
        [Column("Quantity", TypeName = "int")]
        [Required]
        public int Quantity { get; set; }
        [Column("LastUpdated", TypeName = "datetime2")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;    
    }
}
