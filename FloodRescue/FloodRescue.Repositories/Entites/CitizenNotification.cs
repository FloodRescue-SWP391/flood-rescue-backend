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
    [Table("CitizenNotification")]
    public class CitizenNotification
    {
        // 1. Primary Key
        [Key]
        [Column("NotificationID", TypeName = "uniqueidentifier")]
        public Guid NotificationID { get; set; } = Guid.NewGuid();

        // 2. Foreign Key (Trỏ về đơn cứu hộ)
        [Column("RescueRequestID", TypeName = "uniqueidentifier")]
        public Guid RescueRequestID { get; set; }

        [ForeignKey(nameof(RescueRequestID))]
        [JsonIgnore] // Ẩn đi để tránh loop khi trả JSON
        public RescueRequest? RescueRequest { get; set; }

        // 3. Nội dung thông báo
        [MaxLength(100)]
        [Column("Title", TypeName = "nvarchar(100)")]
        public string? Title { get; set; } // Nullable (N)

        [Column("Message", TypeName = "nvarchar(max)")]
        public string? Message { get; set; } // Nullable (N)

        // 4. Thời gian
        [Column("CreatedTime", TypeName = "datetime2(7)")]
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
    }
}
