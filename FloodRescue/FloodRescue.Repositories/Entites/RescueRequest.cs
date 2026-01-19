using Microsoft.EntityFrameworkCore;
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
    [Table("RescueRequests")]
    [Index(nameof(ShortCode), IsUnique = true)] // Đánh Index Unique
    public class RescueRequest
    {
        [Key]
        [Column("RescueRequestID", TypeName = "uniqueidentifier")]
        public Guid RescueRequestID { get; set; } = Guid.NewGuid();

        // Citizen info
        [Column("CitizenName", TypeName = "nvarchar(100)")]
        public string? CitizenName { get; set; } // Nullable (N)

        [Required]
        [MaxLength(15)]
        [Column("CitizenPhone", TypeName = "nvarchar(15)")]
        public string CitizenPhone { get; set; } = string.Empty;

        [Column("Address", TypeName = "nvarchar(225)")]
        public string? Address { get; set; } // Nullable (N)

        // Location & Scale
        [Column("LocationLatitude", TypeName = "float")]
        public double LocationLatitude { get; set; }

        [Column("LocationLongitude", TypeName = "float")]
        public double LocationLongitude { get; set; }

        [Column("PeopleCount", TypeName = "int")]
        public int PeopleCount { get; set; } = 1;

        [Column("Description", TypeName = "nvarchar(max)")]
        public string? Description { get; set; } // Nullable (N)

        // --- LƯU STRING CHO ĐƠN GIẢN ---
        [Required]
        [MaxLength(20)]
        [Column("RequestType", TypeName = "varchar(20)")]
        public string RequestType { get; set; } = "Emergency"; // Emergency, Supply...

        [Required]
        [MaxLength(20)]
        [Column("Status", TypeName = "varchar(20)")]
        public string Status { get; set; } = "Pending"; // Pending, Accepted...

        // Management
        [Required]
        [MaxLength(10)]
        [Column("ShortCode", TypeName = "nvarchar(10)")]
        public string ShortCode { get; set; } = string.Empty;

        [Column("RejectedNote", TypeName = "nvarchar(max)")]
        public string? RejectedNote { get; set; } // Nullable (N)

        [Column("CreatedTime", TypeName = "datetime2(7)")]
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

        // Foreign Key
        [Column("CoordinatorID", TypeName = "uniqueidentifier")]
        public Guid? CoordinatorID { get; set; } // Nullable (N)

        [ForeignKey(nameof(CoordinatorID))]
        [JsonIgnore]
        public User? Coordinator { get; set; }

        [Column("IsDeleted", TypeName = "BIT")]
        public bool IsDeleted { get; set; }
    }


}
