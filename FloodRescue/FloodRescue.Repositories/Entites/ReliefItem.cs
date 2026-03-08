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
    [Table("ReliefItems")]
    public class ReliefItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ReliefItemID", TypeName = "int")]
        public int ReliefItemID { get; set; }

        [Column("ReliefItemName", TypeName = "nvarchar(100)")]
        [Required]
        [MaxLength(100)]
        public string ReliefItemName { get; set; } = string.Empty;


        [Column("CategoryID", TypeName = "int")]
        [Required]
        public int CategoryID { get; set; }

        [ForeignKey(nameof(CategoryID))]
        [JsonIgnore]
        public Category? Category { get; set; }


        // Thay string Unit bằng FK → bảng Units
        // Manager chọn từ dropdown → data đồng nhất 100%
        [Column("UnitID", TypeName = "int")]
        [Required]
        public int UnitID { get; set; }

        [ForeignKey(nameof(UnitID))]
        [JsonIgnore]
        public Unit? Unit { get; set; }

        [Column("IsDeleted", TypeName = "BIT")]
        public bool IsDeleted { get; set; } = false;

    }
}
