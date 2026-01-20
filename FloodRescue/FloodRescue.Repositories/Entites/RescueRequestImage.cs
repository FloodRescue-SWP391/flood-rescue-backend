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
    [Table("RescueRequestImages")]
    public class RescueRequestImage
    {
            [Key]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int ImageId { get; set; } // ID của ảnh thì để int tự tăng cho nhẹ cũng được

            [Required]
            [Column("ImageUrl", TypeName = "nvarchar(max)")]
            public string ImageUrl { get; set; } = string.Empty;

            [Column("Description", TypeName = "nvarchar(255)")]
            public string? Description { get; set; }

            // --- KHÓA NGOẠI (Bắt buộc phải là GUID cho khớp với cha) ---
            [Column("RescueRequestID", TypeName = "uniqueidentifier")]
            public Guid RescueRequestID { get; set; }

            [ForeignKey(nameof(RescueRequestID))]
            [JsonIgnore]
            public RescueRequest? RescueRequest { get; set; }
        }
    }
