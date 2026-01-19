using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace FloodRescue.Repositories.Entites
{
    public class Notification
    {
        [Key]
        [Column("NotificationID", TypeName = "uniqueidentifier")]
        public Guid NotificationID { get; set; } = Guid.NewGuid();
        [Column("UserID", TypeName = "uniqueidentifier")]
        public Guid UserID { get; set; }
        [ForeignKey(nameof(UserID))]
        [JsonIgnore]
        public User? User { get; set; } 
        [Column("ReferenceID", TypeName = "uniqueidentifier")]
        public Guid? ReferenceID { get; set; }
        [Column("Title", TypeName = "nvarchar(100)")]
        [MaxLength(100)]
        public string? Title { get; set; }
        [Column("Message", TypeName = "nvarchar(max)")]
        public string? Message { get; set; }
        [Column("CreatedTime", TypeName = "datetime2")]
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
        [Column("IsRead", TypeName = "BIT")]
        public bool IsRead { get; set; } = false;

    }
}
