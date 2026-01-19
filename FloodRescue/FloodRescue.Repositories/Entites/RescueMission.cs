using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FloodRescue.Repositories.Entites
{
    [Table("RescueMissions")]
    public class RescueMission
    {
        // 1. Primary Key
        [Key]
        [Column("RescueMissionID", TypeName = "uniqueidentifier")]
        
        public Guid RescueMissionID { get; set; } = Guid.NewGuid();

        // 2. Foreign Keys

        // TeamID: Đội nào nhận nhiệm vụ
        [Column("RescueTeamID", TypeName = "uniqueidentifier")]
    
        public Guid RescueTeamID { get; set; }

        [ForeignKey(nameof(RescueTeamID))]
        [JsonIgnore]
        public RescueTeam? RescueTeam { get; set; }

        // RescueRequestID: Nhiệm vụ giải cứu cho đơn nào
        [Column("RescueRequestID", TypeName = "uniqueidentifier")]
        public Guid RescueRequestID { get; set; }

        [ForeignKey(nameof(RescueRequestID))]
        [JsonIgnore] // Tránh loop dữ liệu
        public RescueRequest? RescueRequest { get; set; }

        [Column("Status", TypeName = "varchar(20)")]
        public string Status { get; set; }


        [Column("AssignedAt", TypeName = "datetime2(7)")]
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        // StartTime: Để Nullable theo logic thực tế (Chưa đi chưa có giờ)
        [Column("StartTime", TypeName = "datetime2(7)")]
        public DateTime? StartTime { get; set; }

        [Column("EndTime", TypeName = "datetime2(7)")]
        public DateTime? EndTime { get; set; }

        [Column("IsDeleted", TypeName = "BIT")]
        public bool IsDeleted { get; set; }
    }
}
