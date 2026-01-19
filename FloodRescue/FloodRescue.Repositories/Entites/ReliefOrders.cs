using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FloodRescue.Repositories.Entites
{
    [Table("ReliefOrders")]
    public class ReliefOrder
    {
        // 1. Primary Key
        [Key]
        [Column("ReliefOrderID", TypeName = "uniqueidentifier")]
        public Guid ReliefOrderID { get; set; } = Guid.NewGuid();

        // 2. Foreign Keys

        // Đơn này phục vụ cho Yêu cầu cứu hộ nào?
        [Column("RescueRequestID", TypeName = "uniqueidentifier")]
        public Guid RescueRequestID { get; set; }

        [ForeignKey(nameof(RescueRequestID))]
        [JsonIgnore]
        public RescueRequest? RescueRequest { get; set; }

        // Ai là người quản lý kho tạo đơn này?
        [Column("ManagerID", TypeName = "uniqueidentifier")]
        public Guid ManagerID { get; set; }

        [ForeignKey(nameof(ManagerID))]
        [JsonIgnore]
        public User? Manager { get; set; }

        // Xuất từ kho nào?
        [Column("WarehouseID", TypeName = "int")]
        public int WarehouseID { get; set; }

        [ForeignKey(nameof(WarehouseID))]
        [JsonIgnore]
        public Warehouse? Warehouse { get; set; }

        // Đội nào đi giao? (Nullable vì lúc mới tạo chưa gán đội)
        [Column("RescueTeamID", TypeName = "uniqueidentifier")]
        public Guid? RescueTeamID { get; set; }

        [ForeignKey(nameof(RescueTeamID))]
        [JsonIgnore]
        public RescueTeam? RescueTeam { get; set; }

        // 3. Properties

        [Column("CreatedTime", TypeName = "datetime2(7)")]
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

        [Column("PreparedTime", TypeName = "datetime2(7)")]
        public DateTime? PreparedTime { get; set; } // Nullable (N)

        [Column("Description", TypeName = "nvarchar(max)")]
        public string? Description { get; set; } // Nullable (N)

        // 4. Status (Lưu String cho đơn giản, map Enum ở DTO)
        [Required]
        [MaxLength(50)]
        [Column("Status", TypeName = "nvarchar(50)")]
        public string Status { get; set; } = "Pending";

        [Column("IsDeleted", TypeName = "BIT")]
        public bool IsDeleted { get; set; }
    }
}
